
import { TextInput } from '@equinor/fusion-components';
import { id } from 'date-fns/locale';
import { FC } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';

export type PersonnelFormTextInputProps = {
    item: Personnel;
    onChange: (field: keyof Personnel) => (newValue: string | null) => void;
    field: keyof Personnel;
    disabled: boolean;
    id?: string;
};

const AddPersonnelFormTextInput: FC<PersonnelFormTextInputProps> = ({
    item,
    onChange,
    field,
    disabled,
    id,
}) => {
    return (
        <TextInput
            id = {id}
            disabled={disabled}
            placeholder={item[field]?.toString() || ''}
            key={field + item.personnelId}
            onChange={onChange(field)}
            value={item[field]?.toString() || ''}
        />
    );
};

export default AddPersonnelFormTextInput;
