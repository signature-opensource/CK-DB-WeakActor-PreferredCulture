--[beginscript]

-- Adds the ExtendedCultureId column (idempotent).
if not exists( select *
               from sys.columns
               where object_id = object_id( N'CK.tWeakActor' ) and name = 'ExtendedCultureId' )
begin
    alter table CK.tWeakActor add ExtendedCultureId int not null
        constraint FK_CK_tWeakActor_ExtendedCultureId foreign key( ExtendedCultureId ) references CK.tCulture( CultureId )
        constraint DF_CK_tWeakActor_ExtendedCultureId default( 221272233 );
end

-- Handover from the legacy PreferredLCID column: it referenced the former CK.tLCID table WITHOUT a
-- foreign key, so the CK.DB.Globalization bazooka (that remaps LCID columns to CultureId) missed it.
-- Migrates its values to the new CultureId identifiers, then drops the column.
if exists( select *
           from sys.columns
           where object_id = object_id( N'CK.tWeakActor' ) and name = 'PreferredLCID' )
begin
    -- Legacy LCID -> CultureId mapping (from the bazooka of CK.CultureTable.Install.1.0.0.sql).
    -- Unmapped LCIDs (or cultures no longer registered) keep the column default ('en' = 221272233).
    -- Dynamic SQL is required: DML column binding happens when the whole batch is compiled, and
    -- either PreferredLCID (fresh database) or ExtendedCultureId (legacy database) does not exist yet.
    exec sp_executesql N'
    update w
    set w.ExtendedCultureId = m.CultureId
    from CK.tWeakActor w
    inner join ( values
        (7, 223893631),     -- de
        (4096, 2064397289), -- de-be
        (9, 221272233),     -- en
        (2057, 926936865),  -- en-gb
        (10, 221075630),    -- es
        (12, 210327884),    -- fr
        (1036, 1629332867), -- fr-fr
        (3084, 1621862137), -- fr-ca
        (2060, 1619961366), -- fr-be
        (16, 227957299),    -- it
        (19, 242178626),    -- nl
        (2067, 757164360),  -- nl-be
        (21, 245717780),    -- pl
        (22, 247290620),    -- pt
        (34, 252009142),    -- uk
        (30724, 266820818), -- zh
        (31748, 1319945796),-- zh-hant
        (3076, 960831636),  -- zh-hk
        (1028, 1001725972)  -- zh-tw
    ) m( LCID, CultureId ) on m.LCID = w.PreferredLCID
    where exists( select 1 from CK.tCulture c where c.CultureId = m.CultureId );';

    -- Drops the legacy default constraint: its name is looked up dynamically since it may
    -- differ across projects (Promod used DF_tWeakActor_PreferredLCID).
    declare @df sysname;
    select @df = dc.name
    from sys.default_constraints dc
        inner join sys.columns col on col.default_object_id = dc.object_id
    where dc.parent_object_id = object_id( N'CK.tWeakActor' ) and col.name = 'PreferredLCID';
    if @df is not null exec( N'alter table CK.tWeakActor drop constraint [' + @df + N'];' );

    alter table CK.tWeakActor drop column PreferredLCID;
end

--[endscript]
