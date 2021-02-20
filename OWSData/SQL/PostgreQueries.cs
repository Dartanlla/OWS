using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class PostgreQueries
    {
		public static readonly string GetUserSessionSQL = @"SELECT US.""CustomerGUID"", US.""UserGUID"", US.""UserSessionGUID"", US.""LoginDate"", 
                US.""SelectedCharacterName"",
                U.""FirstName"", U.""LastName"", U.""CreateDate"", U.""LastAccess"", U.""Role""
                FROM ""public"".""UserSessions"" US
                INNER JOIN ""public"".""Users"" U
                    ON U.""UserGUID""=US.""UserGUID""
                WHERE US.""CustomerGUID""=@CustomerGUID
                AND US.""UserSessionGUID""=@UserSessionGUID";

	}
}
