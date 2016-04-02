/* 
 * File:   message.h
 * Author: darksider
 *
 * Created on March 19, 2016, 11:43 PM
 */

#ifndef MESSAGE_H
#define	MESSAGE_H

class message 
{
public:
    message();
    message(const message& orig);
    virtual ~message();
    
    bool save(string from, string to, string message_text);
    bool load(string messages_path);
    bool clean_up();
    
    string time_stamp;
    string user_name;
    string message_text;
    string path;
private:
    
    string get_time_stamp();
};

#endif	/* MESSAGE_H */

