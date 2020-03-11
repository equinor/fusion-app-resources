import * as React from 'react';
import { SearchableDropdown, TextInput, SearchableDropdownOption } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import { BasePosition } from '@equinor/fusion';

export type PersonnelFormDisciplinesDropDown = {
    selection: BasePosition[];
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
            title: s.name,
            key: s.id,
            isSelected: s.name === selectedField,
        }));
    }, [selection, selectedField]);


    const onSelect = React.useCallback((newValue: SearchableDropdownOption) => {
        const changedPerson = { ...item };
        changedPerson.disciplines = [{ name: newValue.title }];
        onChange(changedPerson);
    }, [item])

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
            onSelect={onSelect}
        />
    );
};

export default AddPersonnelFormDisciplinesDropDown;
