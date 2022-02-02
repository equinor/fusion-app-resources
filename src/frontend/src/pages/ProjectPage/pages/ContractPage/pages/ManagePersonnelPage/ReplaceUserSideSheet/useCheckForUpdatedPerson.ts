import { PersonDetails, useApiClients, useNotificationCenter } from '@equinor/fusion';
import { useCallback, useEffect, useState } from 'react';
import Personnel from '../../../../../../../models/Personnel';

const useCheckForUpdatedPerson = (expiredPerson: Personnel) => {
    const [isCheckingForUpdatedPerson, setIsCheckingForUpdatedPerson] = useState<boolean>(false);
    const [updatedPerson, setUpdatedPerson] = useState<PersonDetails | null>(null);
    const [checkForUpdatedPersonError, setCheckForUpdatedPersonError] = useState(null);
    const apiClients = useApiClients();
    const sendNotification = useNotificationCenter();

    const searchForUpdatedPerson = useCallback(
        async (upn: string) => {
            setIsCheckingForUpdatedPerson(true);
            setCheckForUpdatedPersonError(null);
            try {
                const response = await apiClients.people.searchPersons(upn);
                const validResponse = response.data.find((person) => person.upn === upn);
                setUpdatedPerson(validResponse || null);
                !!validResponse &&
                    sendNotification({ level: 'low', title: 'Updated person reference found based on UPN' });
            } catch (e) {
            } finally {
                setIsCheckingForUpdatedPerson(false);
            }
        },
        [apiClients]
    );

    useEffect(() => {
        return () => setUpdatedPerson(null);
    }, []);

    useEffect(() => {
        if (expiredPerson?.upn) {
            searchForUpdatedPerson(expiredPerson.upn);
        }
    }, [expiredPerson, searchForUpdatedPerson]);

    return {
        updatedPerson,
        isCheckingForUpdatedPerson,
        checkForUpdatedPersonError,
        setUpdatedPerson,
    };
};
export default useCheckForUpdatedPerson;
