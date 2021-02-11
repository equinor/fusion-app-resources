
import { TextInput } from '@equinor/fusion-components';
import { FC } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';

export type PersonnelFormTextInputProps = {
    item: Personnel;
    onChange: (field: keyof Personnel) => (newValue: string | null) => void;
    field: keyof Personnel;
    disabled: boolean;
};

const AddPersonnelFormTextInput: FC<PersonnelFormTextInputProps> = ({
    item,
    onChange,
    field,
    disabled,
}) => {
    return (
        <TextInput
            disabled={disabled}
            placeholder={item[field]?.toString() || ''}
            key={field + item.personnelId}
            onChange={onChange(field)}
            value={item[field]?.toString() || ''}
        />
    );
};

export default AddPersonnelFormTextInput;
