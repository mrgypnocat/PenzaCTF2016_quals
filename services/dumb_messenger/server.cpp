/* 
 * File:   server.cpp
 * Author: darksider
 * 
 * Created on February 26, 2016, 7:45 PM
 */

#include "common.h"

server::server(string address, string port) 
{
    this->address       = address;
    this->port          = port;
    
    this->socketfd      = -1;
    this->started       = false;
    
    this->host_info_list = 0;
    this->max_clients_amount = 5;
}

server::server(const server& orig) 
{
}

server::~server() 
{
    if(started)
        stop_server();
    
    if(host_info_list)
        freeaddrinfo(host_info_list);
}

ulong
server::start_server()
{
    ulong status;
    
    addrinfo  host_info;
    
    memset(&host_info,0x0,sizeof(host_info));
    
    host_info.ai_family      = AF_INET;
    host_info.ai_socktype    = SOCK_STREAM;
    //host_info.ai_flags       = AI_PASSIVE;
    
    status = getaddrinfo( address.c_str(),
                          port.c_str(),
                          &host_info,
                          &host_info_list);
    if(status !=0)
    {
        cout<< "getaddrinfo failed. status %d" << status << endl;
        
        return status;
    }
    
    socketfd = socket(  host_info_list->ai_family,
                        host_info_list->ai_socktype,
                        host_info_list->ai_protocol);
    if(socketfd == -1)
    {
        cout << "socket error" << endl;
              
        return -1;
    }
    
    int yes = 1;
    
    setsockopt(socketfd,SOL_SOCKET,SO_REUSEADDR,&yes,sizeof(yes));
    
    signal(SIGCHLD, this->sigchild);
    
    status = bind( socketfd,
                   host_info_list->ai_addr,
                   host_info_list->ai_addrlen);
    
    if(status == -1)
    {
        cout << "Bind error %d" << status << endl;
        
        return status;
    }
    
    started = true;
    
    return 0;
}

void
server::stop_server()
{
    if(!started)
        return;
    
    shutdown(socketfd,SHUT_RDWR);
    close(socketfd);
    
    started = false;
}

void server::process_connections(bool async)
{
    if(!started)
        return;
    
    thread th = thread(&server::connection_thread,this);
    
    if(!async)
        th.join();
    else
        th.detach();
    
    return;
}

void server::connection_thread()
{
    ulong status;
    
    //TODO
    //Перенести и сзапилить объект клиента
    struct sockaddr m_socket;
    socklen_t m_socket_size;
    int client_socketfd;
    
    while("SURPRISE MOTHERFUCKER!")
    {
        //Слушаем
        status = listen(socketfd,max_clients_amount);

        if(status == -1)
        {
            cout << "listen error" << endl;

            return;
        }
        
        client_socketfd = accept( socketfd,
                               &m_socket,
                               &m_socket_size);
        
        /*
        pclient = create_client(pserver);
        
        if(pclient == 0x0)
        {
            printf("Memory error\r\n");
            
            break;
        }
       
        //Прием входящего соединения от клиента
        pclient->m_socketfd = accept( pserver->m_socketfd,
                                      &pclient->m_socket,
                                      &pclient->m_socket_size);

        dbgprintf("New client connected %d\r\n", pclient->m_socketfd);

        setsockopt(pclient->m_socketfd,SOL_SOCKET,SO_REUSEADDR,&yes,sizeof(yes));
        
        if(fork() == 0)
        {            
            exit(0);
        }*/
    }
     
    return;
}

void server::sigchild(int signo)
{
    while (waitpid((pid_t)(-1), 0, WNOHANG) > 0) {}
}