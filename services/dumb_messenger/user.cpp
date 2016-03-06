/* 
 * File:   user.cpp
 * Author: darksider
 * 
 * Created on March 5, 2016, 11:45 PM
 */

#include "common.h"

user::user(string name, string password) 
{
    this->name = name;
    this->password = password;
}

user::user(const user& orig) 
{
}

ulong
user::save()
{
    ulong status = -1;
    
    string pass_hash = md5(password);

    mkdir(name.c_str(), 0777);
    
    ofstream ofs(name + "/pass_hash");    
    
    if(ofs.good())
    {
        ofs << pass_hash << endl;
        ofs.close();
        
        status = 0x0;
    }
    
    return status;
}

ulong
user::login()
{
    ulong status = -1;
    
    string pass_hash = md5(password);
    string saved_pass_hash = "";
    
    ifstream ifs(name + "/pass_hash");    
    
    if(ifs.good())
    {
        ifs >> saved_pass_hash;
        ifs.close();
        
        //давай я как цапля постою - а ты мне погоны отдашь?
        if(saved_pass_hash.compare(pass_hash) == 0x0)
            status = 0x0;
    }
    
    return status;
}

user::~user() 
{
}

