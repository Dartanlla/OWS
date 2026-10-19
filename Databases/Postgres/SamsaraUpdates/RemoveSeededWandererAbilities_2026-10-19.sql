-- New Wanderers no longer get seeded CharAbilities rows.
--
-- AddSamsaraCharacterInitialPersistentData_pg.sql inserted nine fixed rows for every new Wanderer,
-- including learned Mage skills (Ability.Mage.AbruptGale 1, Ability.Mage.MudWall 2) and skills the
-- game no longer has. The game counts every learned row as spent job points, so each new character
-- started 3 points short. The game saves the real starter tree on the first login, so no seed is needed.
--
-- Same function as that script with the CharAbilities insert removed. CREATE OR REPLACE: safe to rerun.
BEGIN;

CREATE OR REPLACE FUNCTION public.addsamsaracharacter(
    _customerguid UUID,
    _usersessionguid UUID,
    _charactername VARCHAR,
    _classname VARCHAR,
    _initialpersistentdata TEXT)
RETURNS TABLE(
    errormessage VARCHAR,
    charactername VARCHAR,
    classname VARCHAR,
    startingmapname VARCHAR,
    x FLOAT8,
    y FLOAT8,
    z FLOAT8,
    rx FLOAT8,
    ry FLOAT8,
    rz FLOAT8,
    teamnumber INT,
    gender INT)
LANGUAGE plpgsql
AS $function$
DECLARE
    _supportunicode BOOLEAN := FALSE;
    _userguid UUID;
    _email VARCHAR(256);
    _classid INT;
    _characterid INT;
    _charinventoryid INT;
    _countofcharnamesfound INT := 0;
    _invalidcharacters INT := 0;
    _startmapname VARCHAR(50);
    _x FLOAT8;
    _y FLOAT8;
    _z FLOAT8;
    _rx FLOAT8;
    _ry FLOAT8;
    _rz FLOAT8;
    _teamnumber INT;
    _gender INT;
    _isinternalnetworktestuser BOOLEAN := FALSE;
BEGIN
    SELECT c.supportunicode
    INTO _supportunicode
    FROM customers c
    WHERE c.customerguid = _customerguid;

    SELECT us.userguid, u.email
    INTO _userguid, _email
    FROM usersessions us
    INNER JOIN users u
        ON u.customerguid = us.customerguid
       AND u.userguid = us.userguid
    WHERE us.customerguid = _customerguid
      AND us.usersessionguid = _usersessionguid;

    SELECT c.classid
    INTO _classid
    FROM class c
    WHERE c.customerguid = _customerguid
      AND c.classname = _classname
    ORDER BY c.classid
    LIMIT 1;

    _charactername := TRIM(_charactername);
    _charactername := REGEXP_REPLACE(_charactername, '\s+', ' ', 'g');
    _invalidcharacters := CASE WHEN _charactername ~ '[^a-zA-Z0-9_ ]' THEN 1 ELSE 0 END;

    SELECT COUNT(*)
    INTO _countofcharnamesfound
    FROM characters c
    WHERE c.customerguid = _customerguid
      AND c.charname = _charactername;

    IF _initialpersistentdata IS NULL OR LENGTH(BTRIM(_initialpersistentdata)) = 0 THEN
        RETURN QUERY SELECT 'InitialPersistentData is required'::VARCHAR,
                            ''::VARCHAR, ''::VARCHAR, ''::VARCHAR,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::INT, 0::INT;
        RETURN;
    END IF;

    IF _invalidcharacters > 0 AND _supportunicode = FALSE THEN
        RETURN QUERY SELECT 'Character Name can only contain letters, numbers, spaces, and underscores'::VARCHAR,
                            ''::VARCHAR, ''::VARCHAR, ''::VARCHAR,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::INT, 0::INT;
        RETURN;
    END IF;

    IF _userguid IS NULL THEN
        RETURN QUERY SELECT 'Invalid User Session'::VARCHAR,
                            ''::VARCHAR, ''::VARCHAR, ''::VARCHAR,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::INT, 0::INT;
        RETURN;
    END IF;

    IF _classid IS NULL THEN
        RETURN QUERY SELECT 'Invalid Class Name'::VARCHAR,
                            ''::VARCHAR, ''::VARCHAR, ''::VARCHAR,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::INT, 0::INT;
        RETURN;
    END IF;

    IF _countofcharnamesfound > 0 THEN
        RETURN QUERY SELECT 'Invalid Character Name'::VARCHAR,
                            ''::VARCHAR, ''::VARCHAR, ''::VARCHAR,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::FLOAT8, 0::FLOAT8, 0::FLOAT8,
                            0::INT, 0::INT;
        RETURN;
    END IF;

    SELECT COALESCE(d.startingmapname, cl.startingmapname),
           COALESCE(d.x, cl.x),
           COALESCE(d.y, cl.y),
           COALESCE(d.z, cl.z),
           COALESCE(d.rx, cl.rx),
           COALESCE(d.ry, cl.ry),
           COALESCE(d.rz, cl.rz),
           cl.teamnumber,
           cl.gender
    INTO _startmapname, _x, _y, _z, _rx, _ry, _rz, _teamnumber, _gender
    FROM class cl
    LEFT JOIN (
        SELECT dcv.startingmapname,
               dcv.x,
               dcv.y,
               dcv.z,
               dcv.rx,
               dcv.ry,
               dcv.rz
        FROM public.defaultsamsaracharactervalues dcv
        WHERE dcv.customerguid = _customerguid
          AND dcv.classid = _classid
        ORDER BY dcv.statidentifier
        LIMIT 1
    ) d ON TRUE
    WHERE cl.customerguid = _customerguid
      AND cl.classid = _classid;

    _isinternalnetworktestuser := RIGHT(_charactername, 4) = '_NTU';

    INSERT INTO characters (customerguid, classid, userguid, email, charname, mapname, x, y, z, perception,
                            acrobatics, climb, stealth, serverip, lastactivity, rx, ry, rz, spirit, magic,
                            teamnumber, thirst, hunger, gold, score, characterlevel, gender, xp, hitdie, wounds,
                            size, weight, maxhealth, health, healthregenrate, maxmana, mana, manaregenrate,
                            maxenergy, energy, energyregenrate, maxfatigue, fatigue, fatigueregenrate, maxstamina,
                            stamina, staminaregenrate, maxendurance, endurance, enduranceregenrate, strength,
                            dexterity, constitution, intellect, wisdom, charisma, agility, fortitude, reflex,
                            willpower, baseattack, baseattackbonus, attackpower, attackspeed, critchance,
                            critmultiplier, haste, spellpower, spellpenetration, defense, dodge, parry, avoidance,
                            versatility, multishot, initiative, naturalarmor, physicalarmor, bonusarmor, forcearmor,
                            magicarmor, resistance, reloadspeed, range, speed, silver, copper, freecurrency,
                            premiumcurrency, fame, alignment, description, isinternalnetworktestuser)
    SELECT _customerguid,
           _classid,
           _userguid,
           _email,
           _charactername,
           _startmapname,
           _x,
           _y,
           _z,
           cl.perception,
           cl.acrobatics,
           cl.climb,
           cl.stealth,
           '',
           NOW(),
           _rx,
           _ry,
           _rz,
           cl.spirit,
           cl.magic,
           cl.teamnumber,
           cl.thirst,
           cl.hunger,
           cl.gold,
           cl.score,
           cl.characterlevel,
           cl.gender,
           cl.xp,
           cl.hitdie,
           cl.wounds,
           cl.size,
           cl.weight,
           cl.maxhealth,
           cl.health,
           cl.healthregenrate,
           cl.maxmana,
           cl.mana,
           cl.manaregenrate,
           cl.maxenergy,
           cl.energy,
           cl.energyregenrate,
           cl.maxfatigue,
           cl.fatigue,
           cl.fatigueregenrate,
           cl.maxstamina,
           cl.stamina,
           cl.staminaregenrate,
           cl.maxendurance,
           cl.endurance,
           cl.enduranceregenrate,
           cl.strength,
           cl.dexterity,
           cl.constitution,
           cl.intellect,
           cl.wisdom,
           cl.charisma,
           cl.agility,
           cl.fortitude,
           cl.reflex,
           cl.willpower,
           cl.baseattack,
           cl.baseattackbonus,
           cl.attackpower,
           cl.attackspeed,
           cl.critchance,
           cl.critmultiplier,
           cl.haste,
           cl.spellpower,
           cl.spellpenetration,
           cl.defense,
           cl.dodge,
           cl.parry,
           cl.avoidance,
           cl.versatility,
           cl.multishot,
           cl.initiative,
           cl.naturalarmor,
           cl.physicalarmor,
           cl.bonusarmor,
           cl.forcearmor,
           cl.magicarmor,
           cl.resistance,
           cl.reloadspeed,
           cl.range,
           cl.speed,
           cl.silver,
           cl.copper,
           cl.freecurrency,
           cl.premiumcurrency,
           cl.fame,
           cl.alignment,
           cl.description,
           _isinternalnetworktestuser
    FROM class cl
    WHERE cl.customerguid = _customerguid
      AND cl.classid = _classid
    RETURNING characterid INTO _characterid;

    INSERT INTO charinventory (customerguid, characterid, inventoryname, inventorysize, inventorywidth, inventoryheight)
    VALUES (_customerguid, _characterid, 'Bag', 16, 4, 4)
    RETURNING charinventoryid INTO _charinventoryid;

    INSERT INTO customcharacterdata (customerguid, characterid, customfieldname, fieldvalue)
    VALUES (_customerguid, _characterid, 'CharacterPersistentData', _initialpersistentdata);

    IF LOWER(_classname) = 'wanderer' THEN
        INSERT INTO charinventoryitems (customerguid, charinventoryid, inslotnumber, quantity, customdata, itemidtag)
        VALUES
            (_customerguid, _charinventoryid, 0, 1, '', 'Item.Id.Equipment.1Handed.Sword.Bronze'),
            (_customerguid, _charinventoryid, 1, 1, '', 'Item.Id.Equipment.Bow.StarterBow'),
            (_customerguid, _charinventoryid, 2, 50, '', 'Item.Id.Equipment.Ammo.Arrow.Bronze');
    END IF;

    RETURN QUERY SELECT ''::VARCHAR,
                        _charactername,
                        _classname,
                        _startmapname,
                        _x,
                        _y,
                        _z,
                        _rx,
                        _ry,
                        _rz,
                        _teamnumber,
                        _gender;
END;
$function$;

GRANT EXECUTE ON FUNCTION public.addsamsaracharacter(UUID, UUID, VARCHAR, VARCHAR, TEXT) TO PUBLIC;

COMMIT;

DO $$
BEGIN
    RAISE NOTICE 'addsamsaracharacter no longer seeds Wanderer ability rows.';
END;
$$;
