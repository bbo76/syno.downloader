## /!\ UNDER DEVELOPMENT /!\

This application allows you to download to your Synology NAS while enjoying the maximum download speed bypassing the Download Station bracket. The download is performed directly by an SSH wget command.

Configuration in the *App.config* file :

     <appSettings>
        <add key="serverIp" value="NAS IP ADDRESS"/>
        <add key="serverPort" value="SSH PORT"/>
        <add key="user" value="DSM user"/>
        <add key="pwd" value="DSM pwd"/>
      </appSettings>
