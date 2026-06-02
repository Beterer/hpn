using Amazon.S3;
using FluentValidation;
using Hpn.Modules.Photo.Contracts;
using Hpn.Modules.Photo.Internal;
using Hpn.Modules.Photo.Internal.AccountData;
using Hpn.Modules.Photo.Internal.Features.DeleteProfilePhoto;
using Hpn.Modules.Photo.Internal.Features.GetMyPhotos;
using Hpn.Modules.Photo.Internal.Features.GetPhotoContent;
using Hpn.Modules.Photo.Internal.Features.GetPublicPhotoContent;
using Hpn.Modules.Photo.Internal.Features.UpdatePhotoOrder;
using Hpn.Modules.Photo.Internal.Features.UploadProfilePhoto;
using Hpn.Modules.Photo.Internal.ImageProcessing;
using Hpn.Modules.Photo.Internal.Nsfw;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Photo.Internal.Storage;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Accounts;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hpn.Modules.Photo;

/// <summary>
/// Composition root for the Photo module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class PhotoModule
{
    public static IServiceCollection AddPhotoModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PhotoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.Configure<PhotoUploadOptions>(configuration.GetSection("Photo"));
        services.Configure<PhotoStorageOptions>(configuration.GetSection("Photo:Storage"));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var storage = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PhotoStorageOptions>>().Value;
            var config = new AmazonS3Config
            {
                ForcePathStyle = storage.ForcePathStyle,
                AuthenticationRegion = storage.Region,
            };

            if (!string.IsNullOrWhiteSpace(storage.ServiceUrl))
            {
                config.ServiceURL = storage.ServiceUrl;
            }

            return new AmazonS3Client(storage.AccessKey, storage.SecretKey, config);
        });

        services.AddScoped<IModuleInitializer, PhotoModuleInitializer>();
        services.AddScoped<IPhotoApi, PhotoApi>();
        services.AddScoped<IProfileActivationRequirement, ReadyPhotoActivationRequirement>();
        services.AddScoped<IObjectStore, S3ObjectStore>();
        services.AddScoped<PhotoUploadValidator>();
        services.AddScoped<ImageProcessor>();
        services.AddScoped<INsfwScanner, NoOpNsfwScanner>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<GetMyPhotosHandler>();
        services.AddScoped<GetPhotoContentHandler>();
        services.AddScoped<GetPublicPhotoContentHandler>();
        services.AddScoped<UploadProfilePhotoHandler>();
        services.AddScoped<DeleteProfilePhotoHandler>();
        services.AddScoped<UpdatePhotoOrderHandler>();
        services.AddScoped<IAccountDataContributor, PhotoDataContributor>();

        services.AddValidatorsFromAssemblyContaining<UpdatePhotoOrderValidator>(ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }

    public static IEndpointRouteBuilder MapPhotoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetMyPhotos();
        endpoints.MapGetPhotoContent();
        endpoints.MapGetPublicPhotoContent();
        endpoints.MapUploadProfilePhoto();
        endpoints.MapDeleteProfilePhoto();
        endpoints.MapUpdatePhotoOrder();

        return endpoints;
    }
}
