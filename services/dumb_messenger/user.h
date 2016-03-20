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
    
    bool refresh_message_list();
    string send_message(string user_name, string message_text);
    
    string get_message(string direction, string message_num);
    
    static string get_name_from_config(string conf_path);
    static bool does_user_exist(string user_name);
    
    user(const user& orig);
    virtual ~user();
    
private:
    
    void cleanup_message_list();
    
    string name;
    string password;
    
    string name_hash;
    string pass_hash;
    
    vector<message*> in_messages;
    vector<message*> out_messages;
};

#endif	/* USER_H */

