type Person = {
    personnelId: string;
    azureUniquePersonId?: string;
    mail: string;
    name: string;
    firstName: string | null;
    lastName: string | null;
    phoneNumber: string;
    jobTitle: string;
    created?: Date | null;
};

export default Person;
