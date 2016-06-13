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
    
    this->in_messages.clear();
    this->out_messages.clear();
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
    
    command = "mkdir -p " + name_hash + "/in_box";
    system(command.c_str());
    
    command = "mkdir -p " + name_hash + "/out_box";
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

bool
user::refresh_message_list()
{
    ulong counter = 0;
    
    string message_path;
    
    DIR           *in_d;
    DIR           *out_d;
    struct dirent *dir;
    
    //да как я вилкой то чистить буду??
    cleanup_message_list();
    
    in_d = opendir((name_hash + "/in_box").c_str());
    if (in_d)
    {
        while ((dir = readdir(in_d)) != NULL)
        {
            if(string(dir->d_name).compare(".")  == 0 ||
               string(dir->d_name).compare("..") == 0 ||
               string(dir->d_name).compare("user_config") == 0)
                continue;
            
            message_path = name_hash + "/in_box/" + string(dir->d_name);
            message* cur_m = new message();
    
            if(cur_m->load(message_path))                
                in_messages.push_back(cur_m);
            else
                delete cur_m;
        }

        closedir(in_d);
    }
    
    out_d = opendir((name_hash + "/out_box").c_str());
    if (out_d)
    {
        while ((dir = readdir(out_d)) != NULL)
        {
            if(string(dir->d_name).compare(".")  == 0 ||
               string(dir->d_name).compare("..") == 0 ||
               string(dir->d_name).compare("user_config") == 0)
                continue;
            
            message_path = name_hash + "/out_box/" + string(dir->d_name);
            message* cur_m = new message();
    
            if(cur_m->load(message_path))                
                out_messages.push_back(cur_m);
            else
                delete cur_m;
        }

        closedir(out_d);
    }
    
    return true;
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
user::get_message(string direction, string message_num)
{
    string answer = "";
    ulong counter = 0;
    
    message* cur_m = NULL;
    
    vector<message*>* message_list = NULL;
    
    istringstream buffer(message_num);
    int value;
    
    if(direction == "in_message")
        message_list = &in_messages;
    if(direction == "out_message")
        message_list = &out_messages;
    
    if(message_list == NULL)
        return "No messagess";
    
    if(message_num == "all")
    {
        for(auto iter = message_list->begin(); iter != message_list->end(); iter++)
        {
            counter++;
            if(direction == "in_message")
                answer += to_string(counter)  + ") "     +
                          (*iter)->time_stamp + " from " +
                          (*iter)->user_name  + "\n";
            else
                answer += to_string(counter)  + ") "   +
                          (*iter)->time_stamp + " to " +
                          (*iter)->user_name  + "\n";
        }
    }
    else
    {
        //Давай шашки из говна сделаем?
        buffer >> value;
        if(value <= 0 || value > message_list->size())
            return "Invalid message num";

        cur_m = message_list->at(value - 1);
        if(cur_m == NULL)
            return "Message parsing error";

        if(direction == "in_message")
            answer = cur_m->time_stamp   + "\nFrom:\n" +
                     cur_m->user_name    + "\nMessage:\n" +
                     cur_m->message_text + "\n";
        else
            answer = cur_m->time_stamp   + "\nTo:\n" +
                     cur_m->user_name    + "\nMessage:\n" +
                     cur_m->message_text + "\n";
    }
    
    return answer;
}

bool
user::remove_message(string direction, string message_num)
{
    message* cur_m = NULL;
    
    vector<message*>* message_list = NULL;
    
    istringstream buffer(message_num);
    int value;
    
    if(direction == "in_message")
        message_list = &in_messages;
    if(direction == "out_message")
        message_list = &out_messages;
    
    if(message_list == NULL)
        return "No messagess";
    
    if(message_num == "all")
    {
        for(auto iter = message_list->begin(); iter != message_list->end(); iter++)
        {
            //Теперь я полковник
            (*iter)->clean_up();
            
            delete *iter;
            
            message_list->erase(iter);
        }
    }
    else
    {
        buffer >> value;
        if(value <= 0 || value > message_list->size())
            return false;

        cur_m = message_list->at(value - 1);
        if(cur_m == NULL)
            return false;

        cur_m->clean_up();
        
        delete cur_m;
    }
    
    return true;
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

void
user::cleanup_message_list()
{
    for(auto iter = in_messages.begin(); iter != in_messages.end(); iter++)
        delete *iter;
    
    for(auto iter = out_messages.begin(); iter != out_messages.end(); iter++)
        delete *iter; 
    
    //Бля у меня так шишка встала....
    in_messages.clear();
    out_messages.clear();
}

user::~user() 
{
    cleanup_message_list();
}

