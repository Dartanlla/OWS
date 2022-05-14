using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class MySQLQueries
    {
        public static readonly string GetUserSessionSQL = @"SELECT US.CustomerGUID, US.UserGUID, US.UserSessionGUID, US.LoginDate, US.SelectedCharacterName,
	            U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role,
	            C.CharacterID, C.CharName, C.X, C.Y, C.Z, C.RX, C.RY, C.RZ, C.MapName as ZoneName
	            FROM UserSessions US
	            INNER JOIN Users U
		            ON U.UserGUID=US.UserGUID
	            LEFT JOIN Characters C
		            ON C.CustomerGUID=US.CustomerGUID
		            AND C.CharName=US.SelectedCharacterName
	            WHERE US.CustomerGUID=@CustomerGUID
	            AND US.UserSessionGUID=@UserSessionGUID";

		public static readonly string GetUserSessionOnlySQL = @"SELECT US.CustomerGUID, US.UserGUID, US.UserSessionGUID, US.LoginDate, US.SelectedCharacterName
	            FROM UserSessions US
	            WHERE US.CustomerGUID=@CustomerGUID
	            AND US.UserSessionGUID=@UserSessionGUID";

		public static readonly string GetUserSQL = @"SELECT U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role
	            FROM Users U
	            WHERE U.CustomerGUID=@CustomerGUID
	            AND U.UserGUID=@UserGUID";

		public static readonly string GetUserFromEmailSQL = @"SELECT U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role
	            FROM Users U
	            WHERE U.CustomerGUID=@CustomerGUID
	            AND U.Email=@Email";

		public static readonly string GetCharacterByNameSQL = @"SELECT C.CharacterID, C.CharName, C.X, C.Y, C.Z, C.RX, C.RY, C.RZ, C.MapName as ZoneName
	            FROM Characters C
	            WHERE C.CustomerGUID=@CustomerGUID
	            AND C.CharName=@CharacterName";
	}
}
