import * as React from 'react';
import {
    SearchableDropdownOption,
    SkeletonBar,
    styling,
    SearchableDropdown,
} from '@equinor/fusion-components';
import { DefaultTableType } from './TableTypes';
import Personnel from '../../../../../../../models/Personnel';

import usePersonnel from '../../../pages/ManagePersonnelPage/hooks/usePersonnel';

function TablePersonnelPicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
}: DefaultTableType<T, Personnel | null>) {
    const selectedPersonnel = React.useMemo(() => accessor(item), [accessor, item]);

    const { personnel, isFetchingPersonnel, personnelError } = usePersonnel();

    const options = React.useMemo((): SearchableDropdownOption[] => {
        return personnel.map(person => ({
            title: person.name,
            key: person.personnelId,
            isSelected: !!(
                selectedPersonnel && person.personnelId === selectedPersonnel.personnelId
            ),
        }));
    }, [personnel, selectedPersonnel]);

    const onDropdownSelect = React.useCallback(
        (option: SearchableDropdownOption) => {
            const person = personnel.find(p => p.personnelId === option.key);
            if (person) {
                onChange(item[rowIdentifier], accessKey, person);
            }
        },
        [onChange, personnel, item, rowIdentifier, accessKey]
    );

    if (isFetchingPersonnel) {
        return <SkeletonBar width="100%" height={styling.grid(7)} />;
    }

    return (
        <SearchableDropdown
            label="Personnel"
            options={options}
            onSelect={onDropdownSelect}
            error={personnelError !== null}
            errorMessage="Unable to get personnel"
        />
    );
}

export default TablePersonnelPicker;
