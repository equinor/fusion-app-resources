import { createContext, useContext } from 'react';
import Personnel from '../../../../../../models/Personnel';

export type ContactMail = {
    personnelId: string;
    preferredContactMail: string | null
}

export type ContactMailCollection = Record<string, ContactMail>;

export interface IManagePersonnelMailContext {
    updateContactMail: (personnelId: string, mail: string) => void;
    isContactMailFormDirty: boolean;
    filteredPersonnel: Personnel[];
    contactMailForm: ContactMailCollection;
    setFilteredPersonnel: (personnel: Personnel[]) => void;
}

const ManagePersonnelMailContext = createContext<IManagePersonnelMailContext>(
    {} as IManagePersonnelMailContext
);

export const useManagePersonnelMailContext  = () => useContext(ManagePersonnelMailContext);

export default ManagePersonnelMailContext.Provider;
