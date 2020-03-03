import * as React from 'react';
import {
    ModalSideSheet,
    Button,
    TextInput,
    DatePicker,
    PersonPicker,
    CheckBox,
    AddIcon,
} from '@equinor/fusion-components';
import useCreatePositionForm from '../hooks/useCreatePositionForm';
import * as styles from '../styles.less';
import { PersonDetails, useNotificationCenter, useCurrentContext } from '@equinor/fusion';
import BasePositionPicker from './BasePositionPicker';
import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';
import CreatePositionRequest from '../../../../../models/createPositionRequest';

type NewPositionSidesheetProps = {
    contract: Contract;
    setCompanyRepPosition: (positionId: string) => void;
    setContractResponsiblePosition: (positionId: string) => void;
    repType: 'company-rep' | 'contract-responsible';
};

const NewPositionSidesheet: React.FC<NewPositionSidesheetProps> = ({
    repType,
    contract,
    setCompanyRepPosition,
    setContractResponsiblePosition,
}) => {
    const [isShowing, setIsShowing] = React.useState(false);

    const {
        formState,
        formFieldSetter,
        setFormField,
        resetForm,
        isFormValid,
        isFormDirty,
    } = useCreatePositionForm();

    const [selectedPerson, setSelectedPerson] = React.useState<PersonDetails | null>(null);
    const onPersonSelect = React.useCallback(
        (person: PersonDetails) => {
            setSelectedPerson(person);
            setFormField('assignedPerson', {
                azureUniqueId: person.azureUniqueId,
                mail: person.mail,
            });
        },
        [setFormField]
    );

    const [alsoUseForOther, setAlsoUseForOther] = React.useState(false);
    const toggleAlsoUseForOther = React.useCallback(() => {
        setAlsoUseForOther(prev => !prev);
    }, []);

    const show = React.useCallback(() => setIsShowing(true), []);
    const onClose = React.useCallback(() => {
        setIsShowing(false);
        resetForm();
    }, []);

    const { apiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const currentContext = useCurrentContext();
    const createExternalCompanyRepAsync = React.useCallback(
        async (request: CreatePositionRequest) => {
            const response = await apiClient.createExternalCompanyReprasentiveAsync(
                currentContext?.id || '',
                contract.id || '',
                request
            );

            const position = response.data;

            setCompanyRepPosition(position.id);

            sendNotification({
                level: 'low',
                title: 'External company rep created',
            });

            return position.id;
        },
        [apiClient, formState]
    );

    const createExternalContractResponsibleAsync = React.useCallback(
        async (request: CreatePositionRequest) => {
            const response = await apiClient.createExternalContractResponsibleAsync(
                currentContext?.id || '',
                contract.id || '',
                request
            );

            const position = response.data;

            setContractResponsiblePosition(position.id);

            sendNotification({
                level: 'low',
                title: 'External contract responsible created',
            });

            return position.id;
        },
        []
    );

    const onSave = React.useCallback(async () => {
        const promises: Promise<string>[] = [];

        const position = { ...formState };
        if (repType === 'company-rep' || alsoUseForOther) {
            promises.push(createExternalCompanyRepAsync(position));
        }

        if (repType === 'contract-responsible' || alsoUseForOther) {
            promises.push(createExternalContractResponsibleAsync(position));
        }

        await Promise.all(promises);

        onClose();
    }, [repType, formState, alsoUseForOther]);

    return (
        <>
            <div className={styles.row}>
                <span>If you can't find your position, try to </span>
                <Button frameless onClick={show}>
                    <AddIcon /> Add new position
                </Button>
            </div>
            <ModalSideSheet
                show={isShowing}
                onClose={onClose}
                size="large"
                header="Add new position to contract"
                headerIcons={[
                    <Button
                        key="save-button"
                        onClick={onSave}
                        disabled={!isFormValid || !isFormDirty}
                    >
                        Save
                    </Button>,
                ]}
            >
                <div className={styles.sideSheetContainer}>
                    <div className={styles.row}>
                        <div className={styles.field}>
                            <BasePositionPicker
                                selectedBasePositionId={formState.basePosition?.id}
                                onSelect={formFieldSetter('basePosition')}
                            />
                        </div>
                        <div className={styles.field}>
                            <TextInput
                                label="Custom position title"
                                value={formState.name}
                                onChange={formFieldSetter('name')}
                            />
                        </div>
                    </div>

                    <div className={styles.row}>
                        <div className={styles.field}>
                            <DatePicker
                                label="From date"
                                selectedDate={formState.appliesFrom}
                                onChange={formFieldSetter('appliesFrom')}
                            />
                        </div>
                        <div className={styles.field}>
                            <DatePicker
                                label="To date"
                                selectedDate={formState.appliesTo}
                                onChange={formFieldSetter('appliesTo')}
                            />
                        </div>
                    </div>

                    <div className={styles.row}>
                        <div className={styles.field}>
                            <PersonPicker
                                label="Person"
                                selectedPerson={selectedPerson}
                                onSelect={onPersonSelect}
                            />
                        </div>
                        <div className={styles.field}>
                            <TextInput
                                label="Workload (%)"
                                value={formState.workload.toString()}
                                onChange={value => setFormField('workload', parseInt(value, 10))}
                            />
                        </div>
                    </div>

                    <label className={styles.row}>
                        <CheckBox selected={alsoUseForOther} onChange={toggleAlsoUseForOther} />{' '}
                        Also use for{' '}
                        <strong>
                            {repType === 'company-rep'
                                ? 'external contract responsible'
                                : 'external company rep'}
                        </strong>
                    </label>
                </div>
            </ModalSideSheet>
        </>
    );
};

export default NewPositionSidesheet;
