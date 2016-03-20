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
    string to_path = md5(to);
    string from_path = md5(from);
    
    bool status = false;
    
    time_stamp = get_time_stamp();
    
    ofstream in_box(to_path + "/in_box/" + md5(from + time_stamp));
    
    if(in_box.good())
    {
        in_box << time_stamp << endl;
        in_box << from << endl;
        in_box << message_text << endl;
        in_box.close();
    }
    else
    {
        return false;
    }
    
    ofstream out_box(from_path + "/out_box/" + md5(to + time_stamp));
    
    if(out_box.good())
    {
        out_box << time_stamp << endl;
        out_box << to << endl;
        out_box << message_text << endl;
        out_box.close();
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