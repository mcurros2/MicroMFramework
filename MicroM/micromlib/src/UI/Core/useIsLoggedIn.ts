import { Dispatch, SetStateAction, useCallback, useEffect, useState } from "react";
import { MicroMClient, MicroMClientClaimTypes } from "../../client";
import { getInitials } from "./loginSupport";


export interface useIsLoggedInProps {
    client: MicroMClient
    useLocalLoggedInCheck?: boolean,
    userInitialsClientClaimsNames?: string[]
}

export type useIsLoggedInReturnType = {
    isLoggedIn: boolean | undefined,
    setIsLoggedIn: Dispatch<SetStateAction<boolean | undefined>>,
    checkIsLoggedIn: () => Promise<void>,
    loggedInInfo: Partial<MicroMClientClaimTypes>,
};

export function useIsLoggedIn({ client, useLocalLoggedInCheck, userInitialsClientClaimsNames }: useIsLoggedInProps): useIsLoggedInReturnType {
    const [isLoggedIn, setIsLoggedIn] = useState<boolean | undefined>(undefined);

    const [loggedInInfo, setLoggedInInfo] = useState<Partial<MicroMClientClaimTypes>>({});

    const checkIsLoggedIn = useCallback(async () => {
        try {
            const result = useLocalLoggedInCheck ? await client.isLoggedInLocal() : await client.isLoggedIn();
            setIsLoggedIn(result);
        } catch (e) {
            setIsLoggedIn(false);
        }
    }, [client, useLocalLoggedInCheck]);


    useEffect(() => {
        async function check() {
            await checkIsLoggedIn();
        }
        check();
    }, [checkIsLoggedIn]);

    useEffect(() => {
        async function getInfo() {
            if (isLoggedIn) {
                const info = client.LOGGED_IN_USER;

                // get intials from the client claims using userInitialsClientClaimsNames. Take the properties values from the client claims it those exist,
                // join them in a string separated with ' ' and use that as the input for the getInitials function
                // if not use the default client.userInitialsInput
                const userInitialsClientClaims = info ? userInitialsClientClaimsNames?.map((name) => info[name]).filter((value) => value as string).join(' ') || info.username || '': '';

                if(info?.username) info.userinitials = getInitials(userInitialsClientClaims);
                setLoggedInInfo(info ?? {});
            }
            else {
                setLoggedInInfo({});
            }
        }
        getInfo();
    }, [isLoggedIn]);


    return {
        isLoggedIn,
        setIsLoggedIn,
        checkIsLoggedIn,
        loggedInInfo,
    };

}