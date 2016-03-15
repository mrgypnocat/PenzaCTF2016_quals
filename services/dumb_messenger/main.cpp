/* 
 * File:   main.cpp
 * Author: darksider
 *
 * Created on February 25, 2016, 7:27 PM
 */
#include "common.h"

/*
 * 
 * Здесь должно быть много умных комментариев
 * Но на них не хватило денег
 */
int main(int argc, char** argv) 
{
    int status;
    
    if(argc < 3)
    {
        cout << "Usage dumb_messenger host port" << endl;
        return -1;
    }
    
    server* srv = new server(argv[1],argv[2]);
    
    status = srv->start_server();
    
    if(status != 0)
    {
        srv->stop_server();
        delete srv;
        
        return status;
    }
    
    srv->process_connections(true);
    
    getchar();
    
    srv->stop_server();
    delete srv;
    
    return 0;
}

