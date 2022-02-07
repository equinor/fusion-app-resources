import { useCurrentContext, useNotificationCenter } from "@equinor/fusion";
import { useState, useCallback, useEffect } from "react";
import { useAppContext } from "../../../../../../../appContext";
import { useContractContext } from "../../../../../../../contractContex";
import Personnel from "../../../../../../../models/Personnel";

const useReplacePersonAccess = (person: Personnel) => {
    const [canReplaceUser, setCanReplaceUser] = useState<boolean>(false);
    const { apiClient } = useAppContext();
    const currentContext = useCurrentContext();
    const { contract } = useContractContext();
    const sendNotification = useNotificationCenter();
    
    const checkReplaceAccess = useCallback(
        async (projectId: string, contractId: string, personId: string) => {
            setCanReplaceUser(false)
            try {
                const headers = await apiClient.getReplacePersonHeadersAsync(
                    projectId,
                    contractId,
                    personId
                );
                const allowHeader = headers.get('Allow');
                if (allowHeader !== null && allowHeader.toLowerCase().indexOf('post') !== -1) {
                    setCanReplaceUser(true)
                }

            } catch (e) {
                sendNotification({
                    level: "low",
                    title:"Unable to get replace person access"
                })
            }
        },
        [apiClient, sendNotification]
    );

    useEffect(() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        const personId = person.azureUniquePersonId;
        if (contractId && projectId && personId) {
            checkReplaceAccess(projectId, contractId, personId);
        }
    }, [contract, currentContext, person]);

    return canReplaceUser
}
export default useReplacePersonAccess