import * as React from 'react';
import {
    SearchableDropdown,
    TextInput,
    SearchableDropdownOption,
} from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import useBasePositions from '../../../../../../../hooks/useBasePositions';

export type PersonnelFormDisciplinesDropDown = {
    onChange: (changedPerson: Personnel) => void;
    item: Personnel;
    disabled: boolean;
};

const AddPersonnelFormDisciplinesDropDown: React.FC<PersonnelFormDisciplinesDropDown> = ({
    onChange,
    item,
    disabled,
}) => {
    const { basePositions, isFetchingBasePositions, basePositionsError } = useBasePositions();

    const options = React.useMemo(() => {
        if (isFetchingBasePositions || basePositionsError) return [];

        const disciplines: SearchableDropdownOption[] = [];
        return basePositions.reduce((d, b): SearchableDropdownOption[] => {
            if (d.some(d => d.key === b.discipline) || !b.discipline.length) return d;

            d.push({
                title: b.discipline,
                key: b.discipline,
                isSelected: b.discipline === item?.disciplines[0]?.name,
            });

            return d;
        }, disciplines);
    }, [basePositions, isFetchingBasePositions, basePositionsError, item]);

    const onSelect = React.useCallback(
        (newValue: SearchableDropdownOption) => {
            const changedPerson = { ...item };
            changedPerson.disciplines = [{ name: newValue.title }];
            onChange(changedPerson);
        },
        [item, onChange]
    );

    if (disabled)
        return (
            <TextInput
                key={`disciplinesDisabled${item.personnelId}`}
                disabled={true}
                placeholder={item.disciplines?.map(d => d.name).join('/') || ''}
                onChange={() => { }}
            />
        );

    return (
        <SearchableDropdown
            key={`searchableDropDown${item.personnelId}`}
            options={options}
            onSelect={onSelect}
        />
    );
};

export default AddPersonnelFormDisciplinesDropDown;
