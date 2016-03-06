/* 
 * File:   client.cpp
 * Author: darksider
 * 
 * Created on February 28, 2016, 2:41 PM
 */

#include "common.h"
#include "user.h"

client::client() 
{
    this->socket_size    = 0;
    this->socketfd       = -1;
    this->socket         = 0;
    this->connected      = false;
    this->in_buffer_size = 0x1000;
    this->in_buffer      = 0x0;
    this->autorized_user = NULL;
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
    
    struct timeval timeout;      
    timeout.tv_sec = 10;
    timeout.tv_usec = 0;
    
    setsockopt ( socketfd, 
                 SOL_SOCKET, 
                 SO_RCVTIMEO, 
                 (char *)&timeout,
                 sizeof(timeout));
    
    in_buffer = (char*)malloc(in_buffer_size);
    
    if(!in_buffer)
        return -1;
    
    connected = true;
    
    return 0;
}

ulong
client::working_thread()
{
    ulong  request_size;
    string request;
    
    string answer;
    
    request_size = 0;
    request = "";
    
    do
    {
        request = read_str();
        //TODO
        //Обработка запроса
        cout << request << endl;
        
        answer = process_request(request);
        
        cout << answer << endl;
        
        write_str(answer);
    }while(request != "quit" && request != "");
        
    return request_size;
}

string 
client::process_user_request(string request)
{
    string user_request = "";
    string user_name    = "";
    string user_pass    = "";
    string answer       = "";
    
    user* cur_user = NULL;
    
    stringstream ss(request);
    
    ss >> user_request;
        
    if(user_request == "get")
    {
        return answer;
    }
    
    ss >> user_name;
    ss >> user_pass;
        
    if(user_name == "" || user_pass == "")
        return "Invalid parameter";
        
    cur_user = new user(user_name,user_pass);
    
    if(user_request == "add")
    {
        if(cur_user->save() == -1)
        {
            delete cur_user;
            cur_user = NULL;
          
            answer = "Add user error";
        }
        else
        {
            answer = "Success";
        }
        
        if(autorized_user)
            delete autorized_user;
        
        autorized_user = cur_user;
        
        return answer;
    }
    
    if(user_request == "login")
    {
        if(cur_user->login() == -1)
        {
            delete cur_user;
            cur_user = NULL;
          
            answer = "Invalid user name or password";
        }
        else
        {
            answer = "Success";
        }
        
        if(autorized_user)
            delete autorized_user;
        
        autorized_user = cur_user;
        
        return answer;
    }

    if(user_request == "remove")
    {
        ss >> user_name;
        ss >> user_pass;
        
        return answer;
    }
    
    return answer;
}

string
client::process_message_request(string request)
{
    string answer = "";
    
    return answer;
}

string
client::process_request(string request)
{
    string answer = "";
    string request_word = "";
    
    stringstream ss(request);
    
    while(ss >> request_word)
    {
        if(request_word == "user")
        {
            string user_request = request.substr(5,request.size());
            answer = process_user_request(user_request);
            break;
        }
        
        if(request_word == "message")
        {
            //if(user == null)
            //{
                //answer == "autorize first";
                //break;
            //}
            string message_request = request.substr(8,request.size());
            answer = process_message_request(ss.str());
            break;
        }
    }
    
    return answer;
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
    
    //writed = write(socketfd,buffer.c_str(),buffer.size());
    writed = send(socketfd,buffer.c_str(),buffer.size(),MSG_NOSIGNAL);
    
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
        
        if(autorized_user)
            delete autorized_user;
    }
}