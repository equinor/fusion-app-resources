import * as React from 'react';
import { SearchableDropdown, TextInput, SearchableDropdownOption } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import useBasePositions from '../../../../../../../hooks/useBasePositions';

export type PersonnelFormDisciplinesDropDown = {
    onChange: (changedPerson: Personnel) => void;
    selectedField: string;
    item: Personnel;
    disabled: boolean;
};

const AddPersonnelFormDisciplinesDropDown: React.FC<PersonnelFormDisciplinesDropDown> = ({
    onChange,
    selectedField,
    item,
    disabled
}) => {

    const { basePositions, isFetchingBasePositions, basePositionsError, } = useBasePositions();

    const options = React.useMemo(() => {
        if (isFetchingBasePositions || basePositionsError)
            return []

        return basePositions.map(s => ({
            title: s.name,
            key: s.id,
            isSelected: s.name === selectedField,
        }));
    }, [basePositions, isFetchingBasePositions, basePositionsError, selectedField]);


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
