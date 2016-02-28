/* 
 * File:   client.cpp
 * Author: darksider
 * 
 * Created on February 28, 2016, 2:41 PM
 */

#include "common.h"

client::client() 
{
    this->socket_size    = 0;
    this->socketfd       = -1;
    this->socket         = 0;
    this->connected      = false;
    this->in_buffer_size = 0x1000;
    this->in_buffer      = 0x0;
}

client::client(const client& orig) 
{
}

ulong
client::connect(sockaddr* client_socket,
                socklen_t client_socket_size,
                int       client_socketfd)
{ 
    if(connected)
        return 0;
    
    if(!client_socket)
        return -1;
    
    socket = (sockaddr*)malloc(client_socket_size);
    
    if(!socket)
        return -1;
    
    memcpy(socket,client_socket,client_socket_size);
    
    socketfd = client_socketfd;
    socket_size = client_socket_size;
    
    int yes = 1;
    setsockopt(socketfd,SOL_SOCKET,SO_REUSEADDR,&yes,sizeof(yes));
    
    in_buffer = (char*)malloc(in_buffer_size);
    
    if(!in_buffer)
        return -1;
    
    connected = true;
    
    return 0;
}

ulong
client::process_request()
{
    ulong  request_size;
    string request;
    
    request_size = 0;
    request = "";
    
    do
    {
        request = read_str();
        //TODO
        //Обработка запроса
        cout << request << endl;
    }while(request != "");
        
    return request_size;
}

string
client::read_str()
{
    int readed;
    string recv_buffer = "";
            
    if(!connected)
        return recv_buffer;
    
    readed = read( socketfd,
                   in_buffer,
                   in_buffer_size);
    
    if(readed != -1)
    {
        recv_buffer += string(in_buffer,readed);
    }
    
    return recv_buffer;
}

int
client::write_str(string buffer)
{
    int writed;
    
    writed = write(socketfd,buffer.c_str(),buffer.size());
    
    return writed;
}

client::~client() 
{
    if(connected)
    {
        if(socket)
            free(socket);
        
        if(in_buffer)
            free(in_buffer);
        
        shutdown(socketfd,SHUT_RDWR);
        close(socketfd);
    }
}