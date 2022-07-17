UPDATE OWSVersion
SET OWSDBVersion='20220613'
WHERE OWSDBVersion IS NOT NULL;

SELECT OWSDBVersion
FROM OWSVersion;

CREATE OR REPLACE PROCEDURE AddOrUpdateAbility(_CustomerGUID UUID,
                                               _AbilityID INT,
                                               _AbilityName VARCHAR(50),
                                               _AbilityTypeID INT,
                                               _TextureToUseForIcon VARCHAR(200),
                                               _Class INT,
                                               _Race INT,
                                               _GameplayAbilityClassName VARCHAR(200),
                                               _AbilityCustomJSON TEXT)
    LANGUAGE PLPGSQL
AS
$$
BEGIN

    IF
        NOT EXISTS(SELECT
                   FROM Abilities AB
                   WHERE AB.CustomerGUID = _CustomerGUID
                     AND (AB.AbilityID = _AbilityID
                       OR AB.AbilityName = _AbilityName)
                       FOR UPDATE) THEN
        INSERT INTO Abilities (CustomerGUID, AbilityName, AbilityTypeID, TextureToUseForIcon, Class, Race,
                               GameplayAbilityClassName, AbilityCustomJSON)
        VALUES (_CustomerGUID, _AbilityName, _AbilityTypeID, _TextureToUseForIcon, _Class, _Race,
                _GameplayAbilityClassName, _AbilityCustomJSON);
    ELSE
        UPDATE Abilities AB
        SET AbilityName              = _AbilityName,
            AbilityTypeID            = _AbilityTypeID,
            TextureToUseForIcon      = _TextureToUseForIcon,
            Class                    = _Class,
            Race                     = _Race,
            GameplayAbilityClassName = _GameplayAbilityClassName,
            AbilityCustomJSON        = _AbilityCustomJSON
        WHERE AB.CustomerGUID = _CustomerGUID
          AND AB.AbilityID = _AbilityID;
    END IF;
END
$$;

CREATE OR REPLACE PROCEDURE AddOrUpdateAbilityType(_CustomerGUID UUID,
                                                   _AbilityTypeID INT,
                                                   _AbilityTypeName VARCHAR(50))
    LANGUAGE PLPGSQL
AS
$$
BEGIN

    IF
        NOT EXISTS(SELECT
                   FROM AbilityTypes ABT
                   WHERE ABT.CustomerGUID = _CustomerGUID
                     AND (ABT.AbilityTypeID = _AbilityTypeID
                       OR ABT.AbilityTypeName = _AbilityTypeName)
                       FOR UPDATE) THEN
        INSERT INTO AbilityTypes (CustomerGUID, AbilityTypeName)
        VALUES (_CustomerGUID, _AbilityTypeName);
    ELSE
        UPDATE AbilityTypes ABT
        SET AbilityTypeName = _AbilityTypeName
        WHERE ABT.CustomerGUID = _CustomerGUID
          AND ABT.AbilityTypeID = _AbilityTypeID;
    END IF;
END
$$;

CREATE OR REPLACE FUNCTION GetAbilityTypes(_CustomerGUID UUID)
    RETURNS TABLE
            (
                AbilityTypeID     INT,
                AbilityTypeName   VARCHAR(50),
                CustomerGUID      UUID,
                NumberOfAbilities INT
            )
    LANGUAGE SQL
AS
$$
SELECT *
     , (SELECT COUNT(*) FROM Abilities AB WHERE AB.AbilityTypeID = ABT.AbilityTypeID) AS NumberOfAbilities
FROM AbilityTypes ABT
WHERE ABT.CustomerGUID = _CustomerGUID
ORDER BY AbilityTypeName;
$$;

CREATE OR REPLACE PROCEDURE AddAbilityToCharacter(_CustomerGUID UUID,
                                                  _AbilityName VARCHAR(50),
                                                  _CharacterName VARCHAR(50),
                                                  _AbilityLevel INT,
                                                  _CharHasAbilitiesCustomJSON TEXT)
    LANGUAGE PLPGSQL
AS
$$
BEGIN
    IF NOT EXISTS(SELECT
                  FROM CharHasAbilities CHA
                           INNER JOIN Characters C
                                      ON C.CharacterID = CHA.CharacterID
                                          AND C.CustomerGUID = CHA.CustomerGUID
                           INNER JOIN Abilities A
                                      ON A.AbilityID = CHA.AbilityID
                                          AND A.CustomerGUID = CHA.CustomerGUID
                  WHERE CHA.CustomerGUID = _CustomerGUID
                    AND C.CharName = _CharacterName
                    AND A.AbilityName = _AbilityName FOR UPDATE) THEN
        INSERT INTO CharHasAbilities (CustomerGUID, CharacterID, AbilityID, AbilityLevel, CharHasAbilitiesCustomJSON)
        SELECT _CustomerGUID AS CustomerGUID,
               (SELECT C.CharacterID
                FROM Characters C
                WHERE C.CharName = _CharacterName AND C.CustomerGUID = _CustomerGUID
                LIMIT 1),
               (SELECT A.AbilityID
                FROM Abilities A
                WHERE A.AbilityName = _AbilityName AND A.CustomerGUID = _CustomerGUID
                LIMIT 1),
               _AbilityLevel,
               _CharHasAbilitiesCustomJSON;
    END IF;
END
$$;