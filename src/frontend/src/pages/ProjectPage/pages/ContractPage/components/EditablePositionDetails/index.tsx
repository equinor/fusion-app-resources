
import Personnel, { PersonnelDiscipline } from '../../../../../../models/Personnel';
import styles from './styles.less';
import {
    PersonPhoto,
    TextInput,
    SearchableDropdownOption,
    SearchableDropdown,
} from '@equinor/fusion-components';
import classNames from 'classnames';
import useBasePositions from '../../../../../../hooks/useBasePositions';
import AzureAdStatusIcon from '../../pages/ManagePersonnelPage/components/AzureAdStatus';
import { PersonDetails } from '@equinor/fusion';
import { ReactNode, FC, useMemo, useCallback } from 'react';

type EditablePositionDetailsProps = {
    person: Personnel;
    edit?: boolean;
    setField?: (field: keyof Personnel) => (value: string | PersonnelDiscipline[]) => void;
};

const getPersonDetails = (person: Personnel): PersonDetails => ({
    azureUniqueId: person.azureUniquePersonId || '',
    name: `${person.firstName} ${person.lastName}`,
    mail: person.mail,
    jobTitle: person.jobTitle,
    department: '',
    mobilePhone: person.phoneNumber,
    officeLocation: '',
    upn: '',
    accountType: 'External',
    company: { id: '', name: '' },
});

const createTextField = (
    label: string,
    text: string | ReactNode,
    textStyle?: string | null
) => {
    return (
        <div className={styles.field}>
            <label>{label}</label>
            <div className={classNames(styles.value, textStyle ? styles[textStyle] : null)}>
                {text}
            </div>
        </div>
    );
};

const createEditField = (
    label: string,
    value: string,
    onChange: (value: string | PersonnelDiscipline[]) => void
) => {
    return (
        <div className={styles.field}>
            <label>{label}</label>
            <TextInput key={label} onChange={onChange} value={value} />
        </div>
    );
};

const EditablePositionDetails: FC<EditablePositionDetailsProps> = ({
    person,
    edit,
    setField,
}) => {
    const { basePositions } = useBasePositions();

    const dropDownOptions = useMemo(() => {
        const disciplineOptions: SearchableDropdownOption[] = [];
        return basePositions.reduce((d, b): SearchableDropdownOption[] => {
            if (d.some((d) => d.key === b.discipline) || !b.discipline.length) return d;

            d.push({
                title: b.discipline,
                key: b.discipline,
                isSelected: b.discipline === person.disciplines[0]?.name,
            });

            return d;
        }, disciplineOptions);
    }, [basePositions, person.disciplines]);

    const onSelect = useCallback(
        (newValue: SearchableDropdownOption) => {
            setField && setField('disciplines')([{ name: newValue.key }]);
        },
        [setField]
    );

    return (
        <div className={styles.container}>
            <div className={styles.personnelDetails}>
                <div className={styles.personPhoto}>
                    <PersonPhoto
                        hideTooltip={true}
                        personId={person.azureUniquePersonId}
                        person={getPersonDetails(person)}
                        size={'xlarge'}
                    />
                </div>
                <div>
                    {edit && setField ? (
                        <>
                            <div className={styles.row}>
                                {createEditField(
                                    'First name',
                                    person.firstName || '',
                                    setField('firstName')
                                )}
                                {createEditField(
                                    'Last name',
                                    person.lastName || '',
                                    setField('lastName')
                                )}
                            </div>
                            <div className={styles.row}>
                                {createTextField('E-Mail', person.mail || '')}
                            </div>
                            <div className={styles.row}>
                                {createEditField(
                                    'Phone',
                                    person.phoneNumber,
                                    setField('phoneNumber')
                                )}
                                {createEditField(
                                    'Dawinci',
                                    person.dawinciCode || '',
                                    setField('dawinciCode')
                                )}
                                {createTextField(
                                    'AD Status',
                                    AzureAdStatusIcon(person.azureAdStatus || 'NoAccount'),
                                    'AdStatus'
                                )}
                            </div>
                        </>
                    ) : (
                        <>
                            <div className={styles.row}>
                                {createTextField('First name', person.firstName || '')}
                                {createTextField('Last name', person.lastName || '')}
                            </div>
                            <div className={styles.row}>
                                {createTextField('E-Mail', person.mail || '')}
                            </div>
                            <div className={styles.row}>
                                {createTextField('Phone', person.phoneNumber || '')}
                                {createTextField('Dawinci', person.dawinciCode || '')}
                                {createTextField(
                                    'AD Status',
                                    AzureAdStatusIcon(person.azureAdStatus || 'NoAccount'),
                                    'AdStatus'
                                )}
                            </div>
                        </>
                    )}
                </div>
            </div>
            <div className={styles.disciplines}>
                <h4>Disciplines</h4>
                {edit ? (
                    <div className={styles.disciplineDropDown}>
                        <SearchableDropdown options={dropDownOptions} onSelect={onSelect} />
                    </div>
                ) : (
                    <div className={styles.disciplinesChips}>
                        {person.disciplines.map((d) => (
                            <div key={d.name} className={styles.disciplineChip}>
                                {d.name}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
};

export default EditablePositionDetails;
