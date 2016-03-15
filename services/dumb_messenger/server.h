/* 
 * File:   server.h
 * Author: darksider
 *
 * Created on February 26, 2016, 7:45 PM
 */

#ifndef SERVER_H
#define	SERVER_H

class server 
{
public:
    server(string address, string port);
    server(const server& orig);
    ~server();
    
    ulong   start_server();
    void    stop_server();
    
    void    process_connections(bool async);
    
private:
    
    void connection_thread();
    
    static void sigchild(int signo);
    
    bool    started;
    
    struct addrinfo*    host_info_list;
    
    string    address;
    string    port;
    
    int socketfd;
    
    ulong max_clients_amount;
    
    string root_dir;
};

#endif	/* SERVER_H */

