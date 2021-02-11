import { useMemo, useCallback } from 'react';
import {
    SearchableDropdownOption,
    SkeletonBar,
    styling,
    SearchableDropdown,
} from '@equinor/fusion-components';
import { DefaultTableType } from './TableTypes';
import Personnel from '../../../../../../../models/Personnel';

function TablePersonnelPicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
    componentState,
}: DefaultTableType<T, Personnel>) {
    const selectedPersonnel = useMemo(() => accessor(item), [accessor, item]);

    const options = useMemo((): SearchableDropdownOption[] => {
        if (!componentState) {
            return [];
        }
        return componentState.data
            .filter(person => person.azureAdStatus === 'Available')
            .map(person => ({
                title: person.name,
                key: person.personnelId,
                isSelected: !!(
                    selectedPersonnel && person.personnelId === selectedPersonnel.personnelId
                ),
            }));
    }, [selectedPersonnel, componentState]);

    const onDropdownSelect = useCallback(
        (option: SearchableDropdownOption) => {
            if (!componentState) {
                return;
            }
            const person = componentState.data.find(p => p.personnelId === option.key);
            if (person) {
                onChange(item[rowIdentifier], accessKey, person);
            }
        },
        [onChange, componentState, item, rowIdentifier, accessKey]
    );

    if (componentState?.isFetching) {
        return <SkeletonBar width="100%" height={styling.grid(7)} />;
    }

    return (
        <SearchableDropdown
            placeholder={columnLabel}
            options={options}
            onSelect={onDropdownSelect}
            error={componentState?.error !== null}
            errorMessage="Unable to get personnel"
        />
    );
}

export default TablePersonnelPicker;
