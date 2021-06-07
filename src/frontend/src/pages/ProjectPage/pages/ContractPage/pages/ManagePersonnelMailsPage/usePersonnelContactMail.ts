import { useCurrentContext } from '@equinor/fusion';
import deepEqual from 'deep-equal';
import { useState, useCallback, useMemo, useEffect } from 'react';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import Personnel from '../../../../../../models/Personnel';
import { ContactMail, ContactMailCollection } from './ManagePersonnelMailContext';

const usePersonnelContactMail = (personnel: Personnel[]) => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();

    const [filteredPersonnel, setFilteredPersonnel] = useState<Personnel[]>([]);
    const [contactMailForm, setContactMailForm] = useState<ContactMailCollection>({});

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

    const isContactMailFormDirty = useMemo(() => {
        return !deepEqual(contactMailForm, defaultFormState);
    }, [contactMailForm, defaultFormState]);

    const updateContactMail = useCallback(
        (personnelId: string, mail: string) => {
            setContactMailForm((form) => ({
                ...form,
                [personnelId]: {
                    personnelId,
                    preferredContactMail: mail,
                },
            }));
        },
        [setContactMailForm]
    );

    const saveContactMailsAsync = useCallback(() => {

    },[])


    return {
        updateContactMail,
        isContactMailFormDirty,
        filteredPersonnel,
        contactMailForm,
        setFilteredPersonnel,
    };
};

export default usePersonnelContactMail;
