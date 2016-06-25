#include <linux/kernel.h>
#include <linux/module.h>
#include <linux/seq_file.h>
#include <linux/slab.h>
#include <linux/proc_fs.h>
#include <linux/sched.h>
#include <linux/list.h>
#include <linux/kthread.h>
#include <asm/ioctls.h>
#include <linux/radix-tree.h>
#include <linux/jhash.h>
#include <linux/limits.h>
#include <net/sock.h>
#include <net/tcp.h>


#define TRACE(fmt, arg...)					\
	printk("%s:%d:%s:%s " fmt "\n",				\
	       (strrchr(__FILE__, '/') ?			\
		strrchr(__FILE__, '/') + 1 : __FILE__),		\
	       __LINE__, __FUNCTION__, current->comm, ##arg)


#define KPASTE_PORT 393

struct kpaste_user {
        char *username;
        char *password;
        char *pastebin;
};

RADIX_TREE(users, GFP_KERNEL);

struct kpaste_service {
        struct socket *sock;
};

static struct kpaste_service kpaste_service;

struct workqueue_struct *wq;

static int kpaste_send(struct socket *sock, char *buf, int len)
{
        struct kvec kv = { .iov_base = buf, .iov_len = len };
        struct msghdr msg = { 0 };

        iov_iter_kvec(&msg.msg_iter, READ | ITER_KVEC, &kv, 1, len);

        return kernel_sendmsg(sock, &msg, &kv, 1, len);
}

static int kpaste_recv_nonblock(struct socket *sock, char *buf, int len)
{
        struct kvec kv = { .iov_base = buf, .iov_len = len };
        struct msghdr msg = { 0 };

        iov_iter_kvec(&msg.msg_iter, READ | ITER_KVEC, &kv, 1, len);

        int ret = kernel_recvmsg(sock, &msg, &kv, 1, len,
                                 MSG_DONTWAIT | MSG_NOSIGNAL);
        if (IS_ERR_VALUE(ret))
                return ret;

        if (ret < len)
                buf[ret] = '\0';

        return ret;
}

static int kpaste_recv(struct socket *sock, char *buf, int len,
                       int timeout, int sleep) /* HZ */
{
        int ret = -EAGAIN;

        while (-EAGAIN == ret && timeout > 0) {
                ret = kpaste_recv_nonblock(sock, buf, len);

                set_current_state(TASK_INTERRUPTIBLE);
                schedule_timeout(sleep);
                timeout -= sleep;
                set_current_state(TASK_RUNNING);
        }

        return ret;
}

struct kpaste_work
{
        struct socket *sock;
};

#define GET_ROOT "GET / HTTP/1.1"
#define POST_PASTE "POST /paste HTTP/1.1"

#define HTTP_200 "HTTP/1.1 200 OK\nContent-Type: text/html; charset=utf-8\n\n"
#define HTTP_404 "HTTP/1.1 404 Not found\nContent-Type: text/html; charset=utf-8\n\n"
#define HTTP_403 "HTTP/1.1 403 Forbidden\nContent-Type: text/html; charset=utf-8\n\n"

#define FMT_HTTP_WITH_COOKIE_200 "HTTP/1.1 200 OK\nSet-Cookie: %s\nContent-Type: text/html; charset=utf-8\n\n"

#define HTML_404 "<!DOCTYPE html>                                       \
<html lang=\"en\">                                                      \
  <head>                                                                \
    <meta charset=\"utf-8\">                                            \
    <title>404 Not found</title>                                        \
  </head>                                                               \
  <body>                                                                \
    <h1>404 Not found</h1>                                              \
  </body>                                                               \
</html>                                                                 \
"

#define HTML_403 "<!DOCTYPE html>                                       \
<html lang=\"en\">                                                      \
  <head>                                                                \
    <meta charset=\"utf-8\">                                            \
    <title>403 Forbidden</title>                                        \
  </head>                                                               \
  <body>                                                                \
    <h1>403 Forbidden</h1>                                              \
  </body>                                                               \
</html>                                                                 \
"

#define HTML_ROOT "<!DOCTYPE html>                                 \
<html lang=\"en\">                                                 \
  <head>                                                           \
    <meta charset=\"utf-8\">                                       \
    <title>kpaste!</title>                                         \
  </head>                                                          \
  <body>                                                           \
    <h1>kpaste!</h1>                                               \
    <section class=\"loginform cf\">                                    \
    <form name=\"login\" action=\"paste\" method=\"post\" accept-charset=\"utf-8\"> \
      <ul>                                                              \
        <li><label for=\"usermail\">Name</label>                        \
        <input type=\"name\" name=\"username\" placeholder=\"name\" required></li> \
        <li><label for=\"password\">Password</label>                    \
        <input type=\"password\" name=\"password\" placeholder=\"password\" required></li> \
        <li>                                                            \
        <input type=\"submit\" value=\"Sign in\"></li>                  \
      </ul>                                                             \
    </form>                                                             \
    </section>                                                          \
  </body>                                                          \
</html>                                                            \
"

#define FMT_HTML_PASTE "<!DOCTYPE html>                                 \
<html lang=\"en\">                                                 \
  <head>                                                           \
    <meta charset=\"utf-8\">                                       \
    <title>kpaste!</title>                                         \
  </head>                                                          \
  <body>                                                           \
    <h1>kpaste!</h1>                                               \
    <form method=\"post\" id=\"testformid\">                            \
      <input type=\"submit\" />                                           \
    </form>                                                             \
    <textarea form =\"testformid\" name=\"text\" id=\"taid\" cols=\"35\" wrap=\"soft\">%s</textarea> \
  </body>                                                               \
</html>                                                                 \
"

static int kpaste_interaction(void *work)
{
        int ret = -ENOMEM;

        struct kpaste_work *kw = (typeof(kw))work;
        struct kpaste_user *user;

        const int buf_len = 1024;
        char* buf = kmalloc(buf_len, GFP_KERNEL);
        if (!buf)
                goto release;

        ret = kpaste_recv(kw->sock, buf, buf_len, HZ*10, HZ/10);
        if (IS_ERR_VALUE(ret))
                goto free_buf;

        int data_len = ret;

        if (!strncmp(buf, GET_ROOT, sizeof(GET_ROOT)-1)) {

                TRACE("GET /");

                ret = kpaste_send(kw->sock, HTTP_200, sizeof(HTTP_200));
                if (IS_ERR_VALUE(ret))
                        goto free_buf;

                ret = kpaste_send(kw->sock, HTML_ROOT, sizeof(HTML_ROOT));
                if (IS_ERR_VALUE(ret))
                        goto free_buf;

        } else if (!strncmp(buf, POST_PASTE, sizeof(POST_PASTE)-1)) {

                TRACE("POST /paste");

                char username[PATH_MAX] = { 0 };
                char password[PATH_MAX] = { 0 };
                char *params = "username=static&password=static";

                int i;
                for (i = data_len-1; i > 0; --i) {
                        if ('\n' == buf[i]) {
                                params = &buf[i+1];
                                break;
                        }
                }

                TRACE("params: %s", params);

                if (!strncmp(params, "username", 8)) {

                        for (i = strnlen(params, PATH_MAX); i > 0; --i) {
                                if ('&' == params[i]) {
                                        sscanf(&params[i+1], "password=%s", password);
                                        params[i] = '\0';
                                        sscanf(params, "username=%s", username);
                                        params[i] = '&';
                                        break;
                                }
                        }

                        TRACE("username: %s", username);
                        TRACE("password: %s", password);

                        user = radix_tree_lookup(
                                &users,
                                jhash(username, strlen(username), 0)
                                );

                        TRACE("jhash: %ld", jhash(username, strlen(username), 0));

                        if (IS_ERR_OR_NULL(user)) {
                                TRACE("");
                                user = kmalloc(sizeof(struct kpaste_user), GFP_KERNEL);
                                user->username = kstrdup(username, GFP_KERNEL);
                                user->password = kstrdup(password, GFP_KERNEL);
                                user->pastebin = "";

                                TRACE("user->username: %s", user->username);
                                TRACE("user->password: %s", user->password);

                                ret = radix_tree_insert(&users,
                                                        jhash(username, strlen(username), 0),
                                                        user);

                                if (IS_ERR_VALUE(ret)) {
                                        kfree(user);
                                        goto shutdown;
                                }

                                const int resp_size = sizeof(FMT_HTTP_WITH_COOKIE_200) + PATH_MAX*16;
                                char *response = kmalloc(resp_size, GFP_KERNEL);
                                snprintf(response, resp_size, FMT_HTTP_WITH_COOKIE_200, user->username);
                                ret = kpaste_send(kw->sock, response, strlen(response));
                                kfree(response);
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;

                                const int size = sizeof(FMT_HTML_PASTE) + PATH_MAX*16;
                                char *paste = kmalloc(size, GFP_KERNEL);
                                snprintf(paste, size, FMT_HTML_PASTE, user->pastebin);
                                ret = kpaste_send(kw->sock, paste, strlen(paste));
                                kfree(paste);
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;

                                goto shutdown;
                        }

                        TRACE("input_password: %s, user_password: %s", password, user->password);
                        if (!strncmp(password, user->password, strlen(password))) {
                                TRACE("");
                                const int resp_size = sizeof(FMT_HTTP_WITH_COOKIE_200) + PATH_MAX*16;
                                char *response = kmalloc(resp_size, GFP_KERNEL);
                                snprintf(response, resp_size, FMT_HTTP_WITH_COOKIE_200, user->username);
                                ret = kpaste_send(kw->sock, response, strlen(response));
                                kfree(response);
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;

                                const int size = sizeof(FMT_HTML_PASTE) + PATH_MAX*16;
                                char *paste = kmalloc(size, GFP_KERNEL);
                                snprintf(paste, size, FMT_HTML_PASTE, user->pastebin);
                                ret = kpaste_send(kw->sock, paste, strlen(paste));
                                kfree(paste);
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;

                                goto shutdown;
                        } else {
                                TRACE("");
                                ret = kpaste_send(kw->sock, HTTP_403, sizeof(HTTP_403));
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;

                                ret = kpaste_send(kw->sock, HTML_403, sizeof(HTML_403));
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;
                        }
                } else if (!strncmp(params, "text", 4)) {
                        int i;
                        char cookie[PATH_MAX] = { 0 };
                        for (i = 0; i < data_len; ++i) {
                                if (!strncmp(&buf[i], "Cookie: ", sizeof("Cookie:"))) {
                                        i += sizeof("Cookie:");
                                        int j;
                                        for (j = i; j < data_len; ++j) {
                                                //TRACE("buf[j]: %c", buf[j]);
                                                if (buf[j] == '\n') {
                                                        buf[j] = '\0';
                                                        strncpy(cookie, &buf[i], PATH_MAX);
                                                        buf[j] = '\n';
                                                        goto found_cookie;
                                                }
                                        }
                                }
                        }

                        TRACE("");

                        ret = kpaste_send(kw->sock, HTTP_403, sizeof(HTTP_403));
                        if (IS_ERR_VALUE(ret))
                                goto free_buf;

                        ret = kpaste_send(kw->sock, HTML_403, sizeof(HTML_403));
                        if (IS_ERR_VALUE(ret))
                                goto free_buf;

                found_cookie:

                        cookie[strnlen(cookie, PATH_MAX)-1] = '\0';

                        TRACE("cookie: '%s'", cookie);

                        user = radix_tree_lookup(
                                &users,
                                jhash(cookie, strlen(cookie), 0)
                                );

                        TRACE("jhash: %ld", jhash(cookie, strlen(cookie), 0));

                        if (!IS_ERR_OR_NULL(user)) {
                                user->pastebin = kstrdup(&params[5], GFP_KERNEL);
                                TRACE("user->pastebin set to %s", user->pastebin);
                                ret = kpaste_send(kw->sock, HTTP_200, sizeof(HTTP_200));
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;

                                ret = kpaste_send(kw->sock, HTML_ROOT, sizeof(HTML_ROOT));
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;
                        } else {
                                ret = kpaste_send(kw->sock, HTTP_403, sizeof(HTTP_403));
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;

                                ret = kpaste_send(kw->sock, HTML_403, sizeof(HTML_403));
                                if (IS_ERR_VALUE(ret))
                                        goto free_buf;
                        }
                } else {
                        ret = kpaste_send(kw->sock, HTTP_200, sizeof(HTTP_200));
                        if (IS_ERR_VALUE(ret))
                                goto free_buf;
                }

        }  else {

                TRACE("404");

                ret = kpaste_send(kw->sock, HTTP_404, sizeof(HTTP_404));
                if (IS_ERR_VALUE(ret))
                        goto free_buf;

                ret = kpaste_send(kw->sock, HTML_404, sizeof(HTML_404));
                if (IS_ERR_VALUE(ret))
                        goto free_buf;
        }

        TRACE("recv: %s", buf);

shutdown:
        kw->sock->ops->shutdown(kw->sock, SHUT_RDWR);
free_buf:
        kfree(buf);
release:
        sock_release(kw->sock);
        kfree(work);
        return ret;
}

static int kpaste_accept(void)
{
        struct socket *client_sock;

        int ret = sock_create(PF_INET, SOCK_STREAM, IPPROTO_TCP, &client_sock);
        if (IS_ERR_VALUE(ret)) {
                TRACE("sock_create: %d", ret);
                goto release;
        }

        ret = kpaste_service.sock->ops->accept(kpaste_service.sock,
                                               client_sock, O_NONBLOCK);
        if (IS_ERR_VALUE(ret))
                goto release;

        TRACE("success accept");

        struct kpaste_work *kw = kmalloc(sizeof(typeof(*kw)), GFP_KERNEL);
        if (!kw) {
                TRACE("enomem");
                ret = -ENOMEM;
                goto release;
        }

        kw->sock = client_sock;

        kthread_run(kpaste_interaction, kw, "kpaste_interaction");
        goto exit;

release:
        sock_release(client_sock);
exit:
        return ret;
}

/* threaded */
static int kpaste_listener(void *unused)
{
        TRACE("");
        int ret;

        while (1) {
                ret = kpaste_accept();
                if (IS_ERR_VALUE(ret) && ret != -EAGAIN) {
                        TRACE("kpaste_accept: %d", ret);                        
                        goto exit;
                }

                set_current_state(TASK_INTERRUPTIBLE);                
                schedule_timeout(HZ/1000);
                set_current_state(TASK_RUNNING);
        }

exit:
        return ret;
}

static int __init kpaste_init(void)
{
	TRACE("");
        int ret;

        wq = create_workqueue("kpaste_queue");
        if (IS_ERR(wq)) {
                ret = -EFAULT;
                TRACE("create_workqueue: %ld", PTR_ERR(wq));
                goto exit;
        }

        struct socket *sock;

        ret = sock_create(PF_INET, SOCK_STREAM, IPPROTO_TCP, &sock);

        if (IS_ERR_VALUE(ret)) {
                TRACE("sock_create: %d", ret);
                goto exit;
        }

        kpaste_service.sock = sock;

        struct sockaddr_in addr = { 0 };
        addr.sin_family = AF_INET;
        addr.sin_addr.s_addr = htonl(INADDR_ANY);
        addr.sin_port = htons(KPASTE_PORT);

        ret = sock->ops->bind(sock, (struct sockaddr *)&addr, sizeof(addr));
        if (IS_ERR_VALUE(ret)) {
                TRACE("bind: %d", ret);
                goto exit;
        }

        ret = sock->ops->listen(sock, SOMAXCONN);
        if (IS_ERR_VALUE(ret)) {
                TRACE("listen: %d", ret);
                goto exit;
        }

        kthread_run(kpaste_listener, NULL, "kpaste_listener");

exit:
	return ret;
}

static void __exit kpaste_exit(void)
{
	TRACE("");
}

module_init(kpaste_init)
module_exit(kpaste_exit)
MODULE_LICENSE("GPL");
