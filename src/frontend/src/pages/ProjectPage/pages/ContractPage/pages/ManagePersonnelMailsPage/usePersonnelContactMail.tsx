import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import deepEqual from 'deep-equal';
import { useState, useCallback, useMemo, useEffect } from 'react';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';
import Personnel from '../../../../../../models/Personnel';
import ResourceError from '../../../../../../reducers/ResourceError';
import { ContactMailCollection } from './ManagePersonnelMailContext';
import SavePersonnelError from './SavePersonnelError';

type PersonnelError = {
    description: string;
    index: number;
};

const pareErrors = (error: any): PersonnelError[] | null => {
    const errors = error?.response?.errors;
    if (!errors) {
        return null;
    }
    return Object.keys(errors).reduce<PersonnelError[]>((prev, curr: string) => {
        const description = errors[curr];
        const openBracetSplittedCurr = curr.split('[');
        if (openBracetSplittedCurr.length < 2) {
            return prev;
        }
        const index = parseInt(openBracetSplittedCurr[1].split(']')[0]);

        if (isNaN(index)) {
            return prev;
        }
        return [...prev, { description, index }];
    }, []);
};

const usePersonnelContactMail = () => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const { contract, dispatchContractAction } = useContractContext();
    const sendNotification = useNotificationCenter();

    const [filteredPersonnel, setFilteredPersonnel] = useState<Personnel[]>([]);
    const [contactMailForm, setContactMailForm] = useState<ContactMailCollection>([]);
    const [isSavingContactMails, setIsSavingContactMails] = useState<boolean>(false);
    const [saveError, setSaveError] = useState<ResourceError | null>(null);
    const [showInputErrors, setShowInputErrors] = useState<boolean>(false);

    const defaultFormState = useMemo(
        (): ContactMailCollection =>
            filteredPersonnel.map((personnel) => ({
                personnelId: personnel.personnelId,
                preferredContactMail: personnel.preferredContactMail || null,
            })),
        [filteredPersonnel]
    );
    useEffect(() => {
        setContactMailForm(defaultFormState);
    }, [defaultFormState]);

    const isContactMailFormDirty = useMemo(() => {
        return !deepEqual(
            contactMailForm.map((c) => ({
                preferredContactMail: c.preferredContactMail,
                personnelId: c.personnelId,
            })),
            defaultFormState
        );
    }, [contactMailForm, defaultFormState]);


    const updateContactMail = useCallback(
        (personnelId: string, mail: string, inputError?: string | null) => {
            const updateContactMail = contactMailForm.map((formItem) =>
                formItem.personnelId !== personnelId
                    ? formItem
                    : { ...formItem, inputError: inputError, preferredContactMail: mail || null }
            );
            setContactMailForm(updateContactMail);
        },
        [setContactMailForm, contactMailForm]
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

        try {
            const response = await apiClient.updatePersonnelPrefferedContactMailsAsync(
                projectId,
                contractId,
                contactMailForm.map((c) => ({
                    preferredContactMail: c.preferredContactMail,
                    personnelId: c.personnelId,
                }))
            );
            dispatchContractAction({ collection: 'personnel', verb: 'set', payload: response });
            sendNotification({
                level: 'low',
                title: 'Preferred contact mails saved',
            });
        } catch (e) {
            const errors = pareErrors(e);
            if (!errors) {
                return;
            }
            const updateMailForm = contactMailForm.map((formItem, index) => {
                const personnelError = errors.find((e) => e.index === index);
                if (!personnelError) {
                    return formItem;
                }
                return {
                    ...formItem,
                    inputError: personnelError.description,
                };
            });
            setContactMailForm(updateMailForm);

            setSaveError(e);
            sendNotification({
                level: 'high',
                title: 'Cannot save contact mails',
                body: (<SavePersonnelError contactMailForm={updateMailForm} />) as any as string,
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
