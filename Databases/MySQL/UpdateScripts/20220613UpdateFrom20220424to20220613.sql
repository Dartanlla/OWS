UPDATE OWSVersion
SET OWSDBVersion='20220613'
WHERE OWSDBVersion IS NOT NULL;

SELECT OWSDBVersion
FROM OWSVersion;

DELIMITER //

CREATE OR REPLACE PROCEDURE AddOrUpdateAbility(_CustomerGUID CHAR(36),
                                               _AbilityID INT,
                                               _AbilityName VARCHAR(50),
                                               _AbilityTypeID INT,
                                               _TextureToUseForIcon VARCHAR(200),
                                               _Class INT,
                                               _Race INT,
                                               _GameplayAbilityClassName VARCHAR(200),
                                               _AbilityCustomJSON TEXT
)
BEGIN
    DECLARE _NewAbilityID INT;

    SELECT AB.AbilityID
    INTO _NewAbilityID
    FROM Abilities AB
    WHERE AB.CustomerGUID = _CustomerGUID
      AND (AB.AbilityID = _AbilityID
        OR AB.AbilityName = _AbilityName);

    IF (_NewAbilityID > 0) THEN
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
    ELSE
        INSERT INTO Abilities (CustomerGUID, AbilityName, AbilityTypeID, TextureToUseForIcon, Class, Race,
                               GameplayAbilityClassName, AbilityCustomJSON)
        VALUES (_CustomerGUID, _AbilityName, _AbilityTypeID, _TextureToUseForIcon, _Class, _Race,
                _GameplayAbilityClassName, _AbilityCustomJSON);
    END IF;
END;

CREATE OR REPLACE PROCEDURE AddOrUpdateAbilityType(_CustomerGUID CHAR(36),
                                                   _AbilityTypeID INT,
                                                   _AbilityTypeName VARCHAR(50)
)
BEGIN
    DECLARE _NewAbilityTypeID INT;

    SELECT ABT.AbilityTypeID
    INTO _NewAbilityTypeID
    FROM AbilityTypes ABT
    WHERE ABT.CustomerGUID = _CustomerGUID
      AND (ABT.AbilityTypeID = _AbilityTypeID
        OR ABT.AbilityTypeName = _AbilityTypeName);

    IF (_NewAbilityTypeID > 0) THEN
        UPDATE AbilityTypes ABT
        SET AbilityTypeName = _AbilityTypeName
        WHERE ABT.CustomerGUID = _CustomerGUID
          AND ABT.AbilityTypeID = _AbilityTypeID;
    ELSE
        INSERT INTO AbilityTypes (CustomerGUID, AbilityTypeName)
        VALUES (_CustomerGUID, _AbilityTypeName);
    END IF;
END;

CREATE OR REPLACE PROCEDURE GetAbilityTypes(_CustomerGUID CHAR(36))
BEGIN

    SELECT *, (SELECT COUNT(*) FROM Abilities AB WHERE AB.AbilityTypeID = ABT.AbilityTypeID) AS NumberOfAbilities
    FROM AbilityTypes ABT
    WHERE ABT.CustomerGUID = _CustomerGUID
    ORDER BY AbilityTypeName;

END;

// DELIMITER ;
