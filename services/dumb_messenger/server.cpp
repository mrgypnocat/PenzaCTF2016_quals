/* 
 * File:   server.cpp
 * Author: darksider
 * 
 * Created on February 26, 2016, 7:45 PM
 */

#include "common.h"

server::server(string address, string port) 
{
    this->root_dir = "dumb_srv/";
    
    this->address       = address;
    this->port          = port;
    
    this->socketfd      = -1;
    this->started       = false;
    
    this->host_info_list = 0;
    //я когда молодой был - 20 раз мог отжаться
    this->max_clients_amount = 20;
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
    
    string command = "mkdir "+ this->root_dir;
    system(command.c_str());
    
    status = chdir(this->root_dir.c_str());
    
    if(status != 0)
    {
        cout << "Create root dir error" << endl;
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
    int status;
    
    struct sockaddr client_socket;
    socklen_t client_socket_size;
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
                                  &client_socket,
                                  &client_socket_size);
        
        
        client* pclient = new client();
        
        if(pclient == 0x0)
        {
            cout << "Memory error" << endl;
            
            continue;
        }
        
        //Братишка - я тебе покушать принес
        status = pclient->connect( &client_socket,
                                    client_socket_size,
                                    client_socketfd);
        
        if(status != 0x0)
        {
            cout << "Connection error" << endl;
            
            delete pclient;
            
            continue;
        }
        /*
        if(fork() == 0)
        {   
            
            pclient->working_thread();
          
            //давай я вот здесь насру - мухи прилетят и мы их убьем
            delete pclient();
            exit(0);
        }
        */
        pclient->working_thread();
        
        delete pclient;
    }
     
    return;
}

void server::sigchild(int signo)
{
    while (waitpid((pid_t)(-1), 0, WNOHANG) > 0) {}
}