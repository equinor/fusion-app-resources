import { useCallback } from 'react';
import CreatePersonnelRequest from '../../../../../../../models/CreatePersonnelRequest';
import { v1 as uuid } from 'uuid';
import useForm from '../../../../../../../hooks/useForm';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import { useMemo } from 'react';

const createDefaultState = (): CreatePersonnelRequest[] => [
    {
        description: '',
        id: uuid(),
        person: null,
        position: null,
    },
];

const useCreateRequestForm = (defaultPersonnelRequests?: PersonnelRequest[] | null) => {
    const transformedDefaultState: CreatePersonnelRequest[] | null = useMemo(
        () =>
            defaultPersonnelRequests
                ? defaultPersonnelRequests.map(defaultState => ({
                      description: defaultState?.description || '',
                      id: defaultState?.id || uuid(),
                      person:
                          defaultState && defaultState.person
                              ? {
                                    azureUniqueId: defaultState.person.azureUniquePersonId || null,
                                    mail: defaultState.person.mail,
                                }
                              : null,
                      position:
                          defaultState && defaultState.position
                              ? {
                                    appliesFrom:
                                        defaultState.position.instances.find(i => i.appliesFrom)
                                            ?.appliesFrom || null,
                                    appliesTo:
                                        defaultState.position.instances.find(i => i.appliesTo)
                                            ?.appliesTo || null,
                                    basePosition: defaultState.position.basePosition || null,
                                    id: defaultState.position.id,
                                    name: defaultState.position.name,
                                    obs:
                                        defaultState.position.instances.find(i => i.obs)?.obs || '',
                                    taskOwnerId: null,
                                    workload:
                                        defaultState.position.instances.find(i => i.workload)
                                            ?.workload || 0,
                                }
                              : null,
                  }))
                : null,
        [defaultPersonnelRequests]
    );
    const validateForm = useCallback((formState: CreatePersonnelRequest[]) => {
        return formState.some(state =>
            Boolean(state.description && state.id && state.person && state.position)
        );
    }, []);

    return useForm(createDefaultState, validateForm, transformedDefaultState);
};

export default useCreateRequestForm;
