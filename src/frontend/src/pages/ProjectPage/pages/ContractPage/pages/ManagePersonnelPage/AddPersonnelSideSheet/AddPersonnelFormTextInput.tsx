import * as React from 'react';
import { TextInput } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';

export type PersonnelFormTextInputProps = {
    item: Personnel;
    onChange: (changedPerson: Personnel) => void;
    field: keyof Personnel;
    disabled: boolean;
};

const AddPersonnelFormTextInput: React.FC<PersonnelFormTextInputProps> = ({
    item,
    onChange,
    field,
    disabled,
}) => {
    return (
        <TextInput
            disabled={disabled}
            placeholder={item[field]?.toString() || ""}
            key={field + item.personnelId}
            onChange={newValue => {
                const changedPerson = { ...item, [field]: newValue };
                onChange(changedPerson);
            }}
            value={item[field]?.toString() || ""}
        />
    );
};

export default AddPersonnelFormTextInput;
