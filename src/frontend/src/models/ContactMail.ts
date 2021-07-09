export type ContactMail = {
    personnelId: string;
    preferredContactMail: string | null
}
export type CreateContactMail = {
    personnel: Array<ContactMail>
}