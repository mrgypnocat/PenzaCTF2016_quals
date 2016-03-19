/* 
 * File:   message.cpp
 * Author: darksider
 * 
 * Created on March 19, 2016, 11:43 PM
 */

#include "common.h"

message::message() 
{
    time_stamp   = "";
    user_name    = "";
    message_text = "";
}

message::message(const message& orig) 
{
}

message::~message() 
{
}

string
message::get_time_stamp()
{
    time_t     now = time(0);
    struct tm  tstruct;
    char       buf[80];
    tstruct = *localtime(&now);
    strftime(buf, sizeof(buf), "%Y-%m-%d.%X", &tstruct);

    return buf;
}

bool
message::save(string from, string to, string message_text)
{
    string path = md5(to);
    bool status = false;
    
    time_stamp = get_time_stamp();
    
    ofstream out_file(path + "/" + md5(from + time_stamp));
    
    if(out_file.good())
    {
        out_file << time_stamp << endl;
        out_file << from << endl;
        out_file << message_text << endl;
        out_file.close();
        status = true;
    }
    
    return status;
}

bool
message::load(string messages_path)
{
    bool status = false;
    
    ifstream in_file(messages_path);
    
    if(in_file.good())
    {
        in_file >> time_stamp;
        in_file >> user_name;
        in_file >> message_text;
        in_file.close();
        status = true;
    }
    
    return status;
}