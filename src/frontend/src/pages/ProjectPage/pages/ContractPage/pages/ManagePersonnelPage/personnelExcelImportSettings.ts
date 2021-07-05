import { v1 as uuid } from 'uuid';
import { ExcelImportSettings } from '../../../../../../hooks/useExcelImport';
import Personnel from '../../../../../../models/Personnel';

const personnelExcelImportSettings: ExcelImportSettings<Personnel> = {
    columns: [
        { title: 'firstName', variations: ['firstname', 'first name'] },
        { title: 'lastName', variations: ['lastname', 'last name'] },
        { title: 'jobTitle', variations: ['jobtitle', 'job title', 'job'] },
        {
            title: 'phoneNumber',
            variations: ['telephone number', 'telephonenumber', 'phonenumber', 'phone number'],
        },
        { title: 'mail', variations: ['mail', 'email', 'e-mail'] },
        { title: 'dawinciCode', variations: ['dawincicode', 'dawinci'] },
        {
            title: 'disciplines',
            variations: ['disciplines', 'discipline'],
            format: (item: string) => {
                return [{ name: item }];
            },
        },
    ],
    autoGenerateColumns: [
        {
            title: 'personnelId',
            format: () => {
                return uuid();
            },
        },
    ],
};

export default personnelExcelImportSettings;
