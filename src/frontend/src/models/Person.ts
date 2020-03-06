type Person = {
    personnelId: string;
    azureUniquePersonId?: string;
    mail: string;
    name: string;
    firstName: string | null;
    lastName: string | null;
    phoneNumber: string;
    jobTitle: string;
};

export default Person;
