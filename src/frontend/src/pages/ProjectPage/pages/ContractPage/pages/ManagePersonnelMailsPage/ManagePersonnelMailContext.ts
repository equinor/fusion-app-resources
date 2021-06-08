import { createContext, useContext } from 'react';
import { ContactMail } from '../../../../../../models/ContactMail';
import Personnel from '../../../../../../models/Personnel';

export type ContactMailCollection = Record<string, ContactMail & { hasInputError?: boolean }>;

export interface IManagePersonnelMailContext {
    updateContactMail: (personnelId: string, mail: string, updateContactMail?: boolean) => void;
    isContactMailFormDirty: boolean;
    filteredPersonnel: Personnel[];
    contactMailForm: ContactMailCollection;
    setFilteredPersonnel: (personnel: Personnel[]) => void;
    isSavingContactMails: boolean;
    saveContactMailsAsync: () => Promise<void>;
    showInputErrors: boolean
}

const ManagePersonnelMailContext = createContext<IManagePersonnelMailContext>(
    {} as IManagePersonnelMailContext
);

export const useManagePersonnelMailContext = () => useContext(ManagePersonnelMailContext);

export default ManagePersonnelMailContext.Provider;
