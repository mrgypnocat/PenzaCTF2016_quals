#include <linux/kernel.h>
#include <linux/module.h>
#include <linux/seq_file.h>
#include <linux/slab.h>
#include <linux/proc_fs.h>
#include <linux/sched.h>
#include <linux/list.h>
#include <linux/kthread.h>
#include <asm/ioctls.h>

#include <net/sock.h>
#include <net/tcp.h>


#define TRACE(fmt, arg...)					\
	printk("%s:%d:%s:%s " fmt "\n",				\
	       (strrchr(__FILE__, '/') ?			\
		strrchr(__FILE__, '/') + 1 : __FILE__),		\
	       __LINE__, __FUNCTION__, current->comm, ##arg)

#define KPASTE_PORT 393

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

static int kpaste_interaction(void *work)
{
        struct kpaste_work *kw = (typeof(kw))work;

        TRACE("kw: %p kw->sock: %p", kw, kw->sock);

        kpaste_send(kw->sock, "Hello!\n", sizeof("Hello!\n"));

        const int buf_len = 1024;
        char* buf = kmalloc(buf_len, GFP_KERNEL);
        TRACE("kpaste_recv: %d", kpaste_recv(kw->sock, buf, buf_len, HZ*10, HZ/10));
        TRACE("recv: %s", buf);

        kfree(buf);

        kw->sock->ops->shutdown(kw->sock, SHUT_RDWR);

        TRACE("");

        sock_release(kw->sock);
        kfree(work);

        return 0;
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
