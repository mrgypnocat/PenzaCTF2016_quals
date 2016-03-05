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
    ulong save();
    user(const user& orig);
    virtual ~user();
private:
    string name;
    string password;
};

#endif	/* USER_H */

