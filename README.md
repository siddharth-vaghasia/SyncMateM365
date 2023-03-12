# Synt Mate M365

Tired of having to manually update your Microsoft 365 calendars across multiple accounts? Look no further than Sync Mate M365!

With Sync Mate M365, you can say goodbye to missed appointments and scheduling conflicts. The app creates a dummy event in each of your calendars whenever a new event is added, updated, or removed in any account. This ensures that all of your calendars are always up to date, no matter which device or account you're using.

Working Demo - [Please visit](https://syncmatem365.azurewebsites.net/). 

[![Hack Together: Microsoft Graph and .NET](https://img.shields.io/badge/Microsoft%20-Hack--Together-orange?style=for-the-badge&logo=microsoft)](https://github.com/microsoft/hack-together)

How it works

![banner2](https://user-images.githubusercontent.com/9557557/224555045-df3f4acf-57ec-41b4-8f8e-3625152660c2.jpeg)


# Screenshots 
Manage Accounts

![image](https://user-images.githubusercontent.com/9557557/224554920-b5f4b703-9e53-458c-8206-fd58e38db921.png)

Consolidated Calendar View
![image](https://user-images.githubusercontent.com/9557557/224554883-749beae4-b9b0-4c2d-9246-0e2c1a42bd83.png)

Meeting details popup
![image](https://user-images.githubusercontent.com/9557557/224554907-38e8b869-fe91-407b-9b0e-e2318587e0fe.png)

# Technical Notes

- App Uses Graph API Change Notifications API.
- Once you login or add new account,App creates a subscription with notification url to our webhook.
- App is using Delegated permission to get all the calendar events and to Create an event.
- App creates mapping between all of your accounts and their respective subscription IDs, which is linked to a unique identifier that is stored during your initial login.
- Azure App Registration with mutli tenant configuration
- Delegated Graph Permissions Calender.Read, Calender.ReadWrite
- App does not store any of calendar data in database.
- Below is list of information stored in to our database(Cloud Mongo DB).
    -    Subscription id for each account
    -    User Principal name (generally email)
    - User Id (Azure AD Unique Identifier)
    - Tenant Id
    - Refresh Token
    - Meeting Id
- A Scheduler job utility to refresh the subscription because as per the design change notfication webhook lifecycle is 3 days.
  
 # Project Structure Information
 - ASP.NET Core MVC Web Application
 - API to connect Graph from backend
 - Scheduler Job Utility to update subscriptions
 - Full Calendar Library for Calendar View

 # Steps to build your our app

 - Clone the repository
 - Create Azure AD App Registration and provided required permission as Calender.Read, Calender.ReadWrite plus basic open id permissions
 - Replace the values in appsettings.json in both Scheduler and Main Solution
 - Create the free mongo db client account https://www.mongodb.com/cloud/atlas/register and create database and collection

    "DatabaseName": "SyncEvent",

    "UserInfoCollectionName": "UserInfo",

    "UserMappingCollectionName": "UserMapping",

    "MeetingMappingCollectionName": "MeetingMapping"
    
- Run the project

Feel free to reach out to us if any help required in setting up the project.
 
# Team

[Siddharth Vaghasia](https://github.com/siddharth-vaghasia)

[Kunj Sangani](https://github.com/kunj-sangani)

[Dharati Patel](https://github.com/dharati1910/)

[Santosh Sarnobat](https://github.com/santoshsarnobat)
