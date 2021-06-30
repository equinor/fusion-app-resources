import { createContext, useContext } from 'react';
import { ContactMail } from '../../../../../../models/ContactMail';
import Personnel from '../../../../../../models/Personnel';

export type ContactMailCollection = Array<ContactMail & { inputError?: string | null }>;

export interface IManagePersonnelMailContext {
    updateContactMail: (personnelId: string, mail: string, inputError?: string | null ) => void;
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
