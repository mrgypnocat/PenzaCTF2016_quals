/* 
 * File:   client.h
 * Author: darksider
 *
 * Created on February 28, 2016, 2:41 PM
 */

#ifndef CLIENT_H
#define	CLIENT_H

class client 
{
public:
    client();
    client(const client& orig);
    virtual ~client();
    
    ulong connect(sockaddr* client_socket,
                  socklen_t client_socket_size,
                  int       client_socketfd);
    ulong working_thread();
private:
    
    string read_str();
    int    write_str(string buffer);
    
    char* in_buffer;
    ulong in_buffer_size;
    
    bool        connected;
    sockaddr*   socket;
    socklen_t   socket_size;
    int         socketfd;
    __time_t    time_out;
    
    user* autorized_user; //--> list<message*> messages;
    
    string process_request(string request);
    string process_user_request(string request);
    string process_message_request(string request);
    //user* add_user(string user_name);
    //давай я здесь насру - мухи слетяться и мы их всех убьем
    //ulong remove_user(user* user);
    //list<user*> get_user_list();
    
};

#endif	/* CLIENT_H */

