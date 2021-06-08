import { CloseCircleIcon, styling, useTooltipRef } from '@equinor/fusion-components';
import { ChangeEvent, FC, useCallback, useMemo } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';
import { useManagePersonnelMailContext } from '../../../ManagePersonnelMailContext';
import * as styles from './styles.less';

type PreferredMailProps = {
    item: Personnel;
};

const emailValidationRegex = /\S+@\S+\.\S+/;

const PreferredMail: FC<PreferredMailProps> = ({ item }) => {
    const { updateContactMail, contactMailForm, showInputErrors } = useManagePersonnelMailContext();

    const invalidMailTooltip = useTooltipRef('Invalid mail');
    const input = useMemo(
        () => contactMailForm[item.personnelId]?.preferredContactMail || '',
        [showInputErrors, contactMailForm]
    );

    const validateInput = useCallback(
        (inputValue: string) =>
            inputValue.length === 0 || emailValidationRegex.test(String(inputValue).toLowerCase()),
        []
    );

    const onPreferredMailChange = useCallback(
        (input: ChangeEvent<HTMLInputElement>) => {
            const inputValue = input.target.value;
            const isValid = validateInput(inputValue);
            updateContactMail(item.personnelId, inputValue, !isValid);
        },
        [validateInput]
    );
    const validationError = useMemo(
        () => contactMailForm[item.personnelId]?.hasInputError && showInputErrors,
        [showInputErrors, contactMailForm]
    );

    return (
        <div className={styles.container}>
            <input className={styles.mailInput} value={input} onChange={onPreferredMailChange} />
            <div className={styles.errorContainer}>
                {validationError && (
                    <div className={styles.error} ref={invalidMailTooltip}>
                        <CloseCircleIcon color={styling.colors.red} />
                    </div>
                )}
            </div>
        </div>
    );
};

export default PreferredMail;
