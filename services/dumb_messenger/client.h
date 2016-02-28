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
    ulong process_request();
private:
    
    string read_str();
    int    write_str(string buffer);
    
    char* in_buffer;
    ulong in_buffer_size;
    
    bool        connected;
    sockaddr*   socket;
    socklen_t   socket_size;
    int         socketfd;
};

#endif	/* CLIENT_H */

