import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
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
    const sendNotification = useNotificationCenter();

    const [filteredPersonnel, setFilteredPersonnel] = useState<Personnel[]>([]);
    const [contactMailForm, setContactMailForm] = useState<ContactMailCollection>({});
    const [isSavingContactMails, setIsSavingContactMails] = useState<boolean>(false);
    const [saveError, setSaveError] = useState<ResourceError | null>(null);
    const [showInputErrors, setShowInputErrors] = useState<boolean>(false);

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
        return !deepEqual(
            Object.values(contactMailForm).map((c) => ({
                preferredContactMail: c.preferredContactMail,
                personnelId: c.personnelId,
            })),
            Object.values(defaultFormState)
        );
    }, [contactMailForm, defaultFormState]);

    const updateContactMail = useCallback(
        (personnelId: string, mail: string, hasInputError?: boolean) => {
            setContactMailForm((form) => ({
                ...form,
                [personnelId]: {
                    personnelId,
                    preferredContactMail: mail || null,
                    hasInputError,
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
        setShowInputErrors(true);
        setIsSavingContactMails(true);
        setSaveError(null);
        const hasInputError = Object.values(contactMailForm).some((c) => !!c.hasInputError);
        if (hasInputError) {
            await sendNotification({
                level: 'high',
                title: 'Input errors',
                body: 'There have been detected invalid emails in the input form, resolve the errors and try again',
            });
            setIsSavingContactMails(false);
            return;
        }
        try {
            const response = await apiClient.updatePersonnelPrefferedContactMailsAsync(
                projectId,
                contractId,
                Object.values(contactMailForm)
                    .filter((c) => !c.hasInputError)
                    .map((c) => ({
                        preferredContactMail: c.preferredContactMail,
                        personnelId: c.personnelId,
                    }))
            );
            sendNotification({
                level: 'low',
                title: 'Preferred contact mails saved',
            });
        } catch (e) {
            console.log(e)
            setSaveError(e);
            sendNotification({
                level: 'high',
                title: 'Cannot save contact mails',
                body: 'An error occurred while saving preferred contact mails',
            });
        } finally {
            setIsSavingContactMails(false);
        }
    }, [contract, currentContext, contactMailForm, sendNotification]);

    return {
        updateContactMail,
        isContactMailFormDirty,
        filteredPersonnel,
        contactMailForm,
        setFilteredPersonnel,
        isSavingContactMails,
        saveContactMailsAsync,
        saveError,
        showInputErrors,
    };
};

export default usePersonnelContactMail;
