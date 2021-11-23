
import {
    SearchableDropdown,
    TextInput,
    SearchableDropdownOption,
} from '@equinor/fusion-components';
import { BasePosition } from '@equinor/fusion';
import Personnel from '../../../../../../../../../models/Personnel';
import PersonnelLine from '../../models/PersonnelLine';
import { FC, useMemo, useCallback } from 'react';

export type PersonnelFormDisciplinesDropDown = {
    onChange: (person: PersonnelLine) => void;
    item: Personnel;
    disabled: boolean;
    basePositions: BasePosition[];
};

const AddPersonnelFormDisciplinesDropDown: FC<PersonnelFormDisciplinesDropDown> = ({
    onChange,
    item,
    disabled,
    basePositions,
}) => {
    const options = useMemo(() => {
        const disciplines: SearchableDropdownOption[] = [];
        return basePositions.reduce((d, b): SearchableDropdownOption[] => {
            if (d.some((d) => d.key === b.discipline) || !b.discipline.length) return d;

            d.push({
                title: b.discipline,
                key: b.discipline,
                isSelected: b.discipline === item?.disciplines[0]?.name,
            });

            return d;
        }, disciplines);
    }, [basePositions, item]);

    const onSelect = useCallback(
        (newValue: SearchableDropdownOption) => {
            onChange({ ...item, disciplines: [{ name: newValue.title }] });
        },
        [item, onChange]
    );

    if (disabled)
        return (
            <TextInput
                key={`disciplinesDisabled${item.personnelId}`}
                disabled={true}
                placeholder={item.disciplines?.map((d) => d.name).join('/') || ''}
                onChange={() => {}}
            />
        );

    return (
        <SearchableDropdown
            id="add-personnel-from-disciplines-dropdown"
            key={`searchableDropDown${item.personnelId}`}
            options={options}
            onSelect={onSelect}
        />
    );
};

export default AddPersonnelFormDisciplinesDropDown;
