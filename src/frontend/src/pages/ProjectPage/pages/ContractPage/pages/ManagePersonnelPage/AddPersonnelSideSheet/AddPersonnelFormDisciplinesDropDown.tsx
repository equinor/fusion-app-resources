import * as React from 'react';
import { SearchableDropdown } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';

export type PersonnelFormDisciplinesDropDown = {
    selection: string[];
    onChange: (changedPerson: Personnel) => void;
    selectedField: string;
    item: Personnel;
};

const AddPersonnelFormDisciplinesDropDown: React.FC<PersonnelFormDisciplinesDropDown> = ({
    selection,
    onChange,
    selectedField,
    item
}) => {
    const options = React.useMemo(() => {
        if (!selection) return []

        return selection.map(s => ({
            title: s,
            key: s,
            isSelected: s === selectedField,
        }));
    }, [selection, selectedField]);

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
