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
    
    string name_hash = md5(name);
    string pass_hash = md5(password);

    mkdir(name_hash.c_str(), 0777);
    
    return status;
}

user::~user() 
{
}

