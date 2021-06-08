import { useCurrentContext } from '@equinor/fusion';
import deepEqual from 'deep-equal';
import { useState, useCallback, useMemo, useEffect } from 'react';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';
import Personnel from '../../../../../../models/Personnel';
import ResourceError from '../../../../../../reducers/ResourceError';
import { ContactMailCollection } from './ManagePersonnelMailContext';

const usePersonnelContactMail = (personnel: Personnel[]) => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const { contract } = useContractContext();

    const [filteredPersonnel, setFilteredPersonnel] = useState<Personnel[]>([]);
    const [contactMailForm, setContactMailForm] = useState<ContactMailCollection>({});
    const [isSavingContactMails, setIsSavingContactMails] = useState<boolean>(false);
    const [saveError, setSaveError] = useState<ResourceError | null>(null);

    const defaultFormState = useMemo(
        () =>
            personnel.reduce<ContactMailCollection>(
                (prev, curr) =>
                    !curr.azureUniquePersonId
                        ? prev
                        : {
                              ...prev,
                              [curr.personnelId]: {
                                  personnelId: curr.personnelId,
                                  preferredContactMail: curr.preferredContactMail || null,
                              },
                          },
                {}
            ),
        [personnel]
    );
    
    useEffect(() => {
        setContactMailForm(defaultFormState);
    }, [defaultFormState]);

    const isContactMailFormDirty = useMemo(() => {
        return !deepEqual(contactMailForm, defaultFormState);
    }, [contactMailForm, defaultFormState]);

    const updateContactMail = useCallback(
        (personnelId: string, mail: string) => {
            setContactMailForm((form) => ({
                ...form,
                [personnelId]: {
                    personnelId,
                    preferredContactMail: mail || null,
                },
            }));
        },
        [setContactMailForm]
    );

    const saveContactMailsAsync = useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return;
        }
        setIsSavingContactMails(true);
        setSaveError(null);
        try {
            const response = await apiClient.updatePersonnelPrefferedContactMailsAsync(
                projectId,
                contractId,
                Object.values(contactMailForm)
            );
        } catch (e) {
            setSaveError(e);
        } finally {
            setIsSavingContactMails(false);
        }
    }, [contract, currentContext, contactMailForm]);

    return {
        updateContactMail,
        isContactMailFormDirty,
        filteredPersonnel,
        contactMailForm,
        setFilteredPersonnel,
        isSavingContactMails,
        saveContactMailsAsync,
        saveError,
    };
};

export default usePersonnelContactMail;
