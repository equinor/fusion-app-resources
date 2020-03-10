import * as React from 'react';
import { SearchableDropdown, TextInput } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';

export type PersonnelFormDisciplinesDropDown = {
    selection: string[];
    onChange: (changedPerson: Personnel) => void;
    selectedField: string;
    item: Personnel;
    disabled: boolean;
};

const AddPersonnelFormDisciplinesDropDown: React.FC<PersonnelFormDisciplinesDropDown> = ({
    selection,
    onChange,
    selectedField,
    item,
    disabled
}) => {
    const options = React.useMemo(() => {
        if (!selection) return []

        return selection.map(s => ({
            title: s,
            key: s,
            isSelected: s === selectedField,
        }));
    }, [selection, selectedField]);

    if (disabled)
        return <TextInput
            key={`disciplines${item.personnelId}`}
            disabled={true}
            placeholder={item.disciplines?.map(d => d.name).join('/') || ""}
            onChange={() => { }}
        />

    return (
        <SearchableDropdown
            options={options}
            onSelect={newValue => {
                const changedPerson = { ...item };
                changedPerson.disciplines = [{ name: newValue.key }];
                onChange(changedPerson);
            }}
        />
    );
};

export default AddPersonnelFormDisciplinesDropDown;
