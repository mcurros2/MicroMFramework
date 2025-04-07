create or alter proc usr_brwStandard
        @user_id Char(20)
        , @like VarChar(max)
        , @d Char(1)
        as

set @like = NULLIF(@like,'')

create table [#like] (phrase varchar(max))

IF @like IS NOT NULL
BEGIN
    insert  [#like]
    select  phrase
    from    openjson(@like) WITH (phrase varchar(max) '$') a
END

select  [User Id] = rtrim(a.c_user_id)
        , [Username] = a.vc_username
        , [Email] = a.vc_email
        , [Bad logon attempts] = a.i_badlogonattempts
        , [Disabled] = a.bt_disabled
        , [Locked] = a.dt_locked
        , [Last Login] = a.dt_last_login
        , [Last Refresh] = a.dt_last_refresh
from    [microm_users] a
where   
        not exists (
            select  1
            from    [#like] l
            where   not 
            (
                a.c_user_id like l.phrase
                or a.vc_username like l.phrase
                or a.vc_email like l.phrase
            )
        )