/* 
 * File:   user.cpp
 * Author: darksider
 * 
 * Created on March 5, 2016, 11:45 PM
 */

#include "common.h"

user::user(string name, string password) 
{    
    this->name     = name;
    this->password = password;
    
    this->name_hash = md5(name);
    this->pass_hash = md5(password);
    
    this->message_list.clear();
}

user::user(const user& orig) 
{
}

bool
user::save()
{
    bool status = false;
    
    string command = "mkdir -p " + name_hash;
    
    system(command.c_str());
    
    ofstream ofs(name_hash + "/user_config");    
    
    if(ofs.good())
    {
        ofs << name << endl;
        ofs << pass_hash << endl;
        ofs.close();
        
        status = true;
    }
    else
    {
        cleanup();
    }
    
    return status;
}

bool
user::login()
{
    bool status = false;
    
    string name = "";
    string saved_pass_hash = "";
    
    ifstream ifs(name_hash + "/user_config");    
    
    if(ifs.good())
    {
        ifs >> name;
        ifs >> saved_pass_hash;
        ifs.close();
        
        //давай я как цапля постою - а ты мне погоны отдашь?
        if(saved_pass_hash.compare(pass_hash) == 0x0)
            status = true;
    }
    
    return status;
}

bool
user::cleanup()
{
    bool status = false;
    
    string command ="rm -r " + name_hash;
    
    status = system(command.c_str());
    
    return status == 0 ? true : false;
}

string
user::get_name()
{
    return name;
}

string
user::get_message_list()
{
    ulong counter = 0;
    
    string answer = "";
    string message_path;
    
    DIR           *d;
    struct dirent *dir;
    
    d = opendir(name_hash.c_str());
    if (d)
    {
        while ((dir = readdir(d)) != NULL)
        {
            if(string(dir->d_name).compare(".")  == 0 ||
               string(dir->d_name).compare("..") == 0 ||
               string(dir->d_name).compare("user_config") == 0)
                continue;
            
            message_path = name_hash + "/" + string(dir->d_name);
            message* cur_m = new message();
    
            if(cur_m->load(message_path))
            {
                counter++;
                
                answer += to_string(counter) + ") " +
                          cur_m->time_stamp  + " " +
                          cur_m->user_name;
                
                answer += "\r\n";
                
                message_list.push_back(cur_m);
            }
            else
            {
                delete cur_m;
            }
        }

        closedir(d);
    }
    
    return answer;
}

string
user::send_message(string dest_user_name, string message_text)
{
    string user_dir = md5(dest_user_name);
    
    if(!user::does_user_exist(dest_user_name))
        return "User does not exist";
    
    message m = message();
    
    if(!m.save(name,dest_user_name,message_text))
        return "Send message error";
    
    return "Success";
}

string
user::get_name_from_config(string conf_path)
{
    string name = "";
    
    ifstream ifs(conf_path + "/user_config");    
    
    if(ifs.good())
    {
        ifs >> name;
        ifs.close();
    }
    
    return name;
}

bool
user::does_user_exist(string user_name)
{
    string conf_path = md5(user_name);
    bool exist;
    
    ifstream ifs(conf_path + "/user_config");
    
    if(ifs.good())
    {
        exist = true;
        ifs.close();
    }
    else
    {
        exist = false;
    }
    
    return exist;
}

user::~user() 
{
    for(auto iter = message_list.begin(); iter != message_list.end(); iter++)
        delete *iter;        
        
    message_list.clear();
}

