/* 
 * File:   user.h
 * Author: darksider
 *
 * Created on March 5, 2016, 11:45 PM
 */

#ifndef USER_H
#define	USER_H

class user 
{
public:
    user(string name, string password);
    bool save();
    bool login();
    bool cleanup();
    string get_name();
    
    string get_message_list();
    string send_message(string user_name, string message_text);
    
    static string get_name_from_config(string conf_path);
    static bool does_user_exist(string user_name);
    
    user(const user& orig);
    virtual ~user();
    
private:    
    string name;
    string password;
    
    string name_hash;
    string pass_hash;
    
    list<message*> message_list;
};

#endif	/* USER_H */

