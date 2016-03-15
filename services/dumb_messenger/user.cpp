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
}

user::user(const user& orig) 
{
}

ulong
user::save()
{
    ulong status = -1;
    
    string command = "mkdir -p " + name_hash;
    
    system(command.c_str());
    
    ofstream ofs(name_hash + "/user_config");    
    
    if(ofs.good())
    {
        ofs << name << endl;
        ofs << pass_hash << endl;
        ofs.close();
        
        status = 0x0;
    }
    else
    {
        cleanup();
    }
    
    return status;
}

ulong
user::login()
{
    ulong status = -1;
    
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
            status = 0x0;
    }
    
    return status;
}

ulong
user::cleanup()
{
    ulong status = -1;
    
    string command ="rm -r " + name_hash;
    
    status = system(command.c_str());
    
    return status == 0 ? status : -1;
}

string
user::get_name()
{
    return name;
}

user::~user() 
{
}

