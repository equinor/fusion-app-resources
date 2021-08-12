import { Button } from '@equinor/fusion-components';
import { FC, useCallback, useEffect, useMemo, useState } from 'react';
import Personnel from '../../../../../../../models/Personnel';
import styles from './styles.less';

type ToolbarFilterProps = {
    personnel: Personnel[];
    setFilteredPersonnel: (filteredPersonnel: Personnel[]) => void;
    filteredPersonnel: Personnel[];
};
type FilterKeys = 'noMail' | 'showMissingAD';
type PersonnelFilter = (item: Personnel) => boolean;
type Filters = {
    filter: PersonnelFilter;
    isSelected?: boolean;
};
type SelectedFilters = Record<FilterKeys, Filters>;

const filterDefinitions: SelectedFilters = {
    noMail: {
        filter: (p) => !p.mail && !p.preferredContactMail,
    },
    showMissingAD: {
        filter: (p) => p.azureAdStatus !== 'NoAccount',
        isSelected: true,
    },
};
const ToolbarFilter: FC<ToolbarFilterProps> = ({
    personnel,
    setFilteredPersonnel,
    filteredPersonnel,
}) => {
    const [selectedFilters, setSelectedFilters] = useState<SelectedFilters>(filterDefinitions);

    const personnelWithNoMail = useMemo(
        () => filteredPersonnel.filter(filterDefinitions.noMail.filter).length,
        [filteredPersonnel]
    );

    const personnelWithNoAd = useMemo(
        () => personnel.filter((p) => !filterDefinitions.showMissingAD.filter(p)).length,
        [personnel]
    );

    const filterPersonnel = useCallback(() => {
        const activeFilters = Object.values(selectedFilters)
            .filter((f) => !!f.isSelected)
            .map((f) => f.filter);
        if (activeFilters?.length === 0) {
            setFilteredPersonnel(personnel);
            return;
        }
        setFilteredPersonnel(personnel.filter((p) => activeFilters.every((filter) => filter(p))));
    }, [personnel, selectedFilters, setFilteredPersonnel]);

    const toggleSelected = useCallback(
        (filterKey: FilterKeys) => {
            const filter = selectedFilters[filterKey];
            setSelectedFilters({
                ...selectedFilters,
                [filterKey]: {
                    ...filter,
                    isSelected: !filter?.isSelected,
                },
            });
        },
        [selectedFilters, setSelectedFilters]
    );

    const onFilterNoMail = useCallback(() => toggleSelected('noMail'), [toggleSelected]);
    const onShowAD = useCallback(() => toggleSelected('showMissingAD'), [toggleSelected]);

    useEffect(() => {
        filterPersonnel();
    }, [selectedFilters, personnel]);

    return (
        <div className={styles.toolbar}>
            <div className={styles.toolbarItem}>
                <Button
                    outlined={!selectedFilters.noMail.isSelected}
                    onClick={onFilterNoMail}
                    disabled={personnelWithNoMail === 0}
                >
                    Personnel with no mail ({personnelWithNoMail})
                </Button>
            </div>
            <div className={styles.toolbarItem}>
                <Button outlined={selectedFilters.showMissingAD.isSelected} onClick={onShowAD}>
                    {selectedFilters.showMissingAD.isSelected
                        ? `Show personnel with missing AD (${personnelWithNoAd})`
                        : `Hide personnel with missing AD (${personnelWithNoAd})`}
                </Button>
            </div>
        </div>
    );
};

export default ToolbarFilter;
